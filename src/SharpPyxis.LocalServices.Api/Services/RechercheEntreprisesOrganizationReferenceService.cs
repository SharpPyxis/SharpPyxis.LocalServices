using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpPyxis.LocalServices.Api.Models;

namespace SharpPyxis.LocalServices.Api.Services;

/// <summary>
/// Uses the French Recherche Entreprises API to normalize organization data.
/// </summary>
public sealed class RechercheEntreprisesOrganizationReferenceService : IOrganizationReferenceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RechercheEntreprisesOrganizationReferenceService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes the service with its HTTP client and logger.
    /// </summary>
    public RechercheEntreprisesOrganizationReferenceService(
        HttpClient httpClient,
        ILogger<RechercheEntreprisesOrganizationReferenceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Queries the external registry and maps the response to the local normalization model.
    /// </summary>
    public async Task<OrganizationNormalizeResult> NormalizeAsync(
        OrganizationProbe probe,
        CancellationToken cancellationToken = default)
    {
        var result = new OrganizationNormalizeResult
        {
            Probe = probe,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        };

        var query = BuildQuery(probe);
        if (string.IsNullOrWhiteSpace(query))
        {
            result.Status = "Error";
            result.Message = "No usable data in probe to build a query.";
            return result;
        }

        // Limit the response size so downstream consumers stay lightweight.
        var url = $"search?q={Uri.EscapeDataString(query)}&per_page=5&page=1";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calling Recherche Entreprises API with query {Query}", query);
            result.Status = "Error";
            result.Message = $"Error calling external API: {ex.Message}";
            return result;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Recherche Entreprises returned {StatusCode} for query {Query}. Body: {Body}",
                (int)response.StatusCode, query, body);

            result.Status = "Error";
            result.Message = $"External API returned {(int)response.StatusCode}";
            return result;
        }

        ReSearchResponse? payload;
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            payload = await JsonSerializer.DeserializeAsync<ReSearchResponse>(stream, JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize Recherche Entreprises response for query {Query}", query);
            result.Status = "Error";
            result.Message = "Failed to deserialize external API response.";
            return result;
        }

        if (payload is null)
        {
            result.Status = "Error";
            result.Message = "Empty response from external API.";
            return result;
        }

        if (payload.TotalResults <= 0 || payload.Results is null || payload.Results.Count == 0)
        {
            result.Status = "None";
            result.Message = "No organization found for the given query.";
            return result;
        }

        // Map the external payload to the local candidate model.
        foreach (var item in payload.Results)
        {
            var candidate = MapCandidate(item);
            result.Candidates.Add(candidate);
        }

        // Use the first result for now and refine the selection strategy later if needed.
        result.BestMatch = result.Candidates.FirstOrDefault();

        result.Status = payload.TotalResults switch
        {
            1 => "Single",
            > 1 => "Multiple",
            _ => "Unknown"
        };

        result.Message = $"Found {payload.TotalResults} candidates (page {payload.Page}/{payload.TotalPages}).";

        return result;
    }

    private static string BuildQuery(OrganizationProbe probe)
    {
        // Si SIREN/SIRET sont présents, c’est le plus sélectif
        if (!string.IsNullOrWhiteSpace(probe.Siret))
        {
            return probe.Siret.Trim();
        }

        if (!string.IsNullOrWhiteSpace(probe.Siren))
        {
            return probe.Siren.Trim();
        }

        // Sinon on compose une chaîne texte à partir des infos disponibles
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(probe.Name))
            parts.Add(probe.Name!.Trim());

        if (!string.IsNullOrWhiteSpace(probe.TradeName))
            parts.Add(probe.TradeName!.Trim());

        if (!string.IsNullOrWhiteSpace(probe.AddressLine1))
            parts.Add(probe.AddressLine1!.Trim());

        if (!string.IsNullOrWhiteSpace(probe.PostalCode))
            parts.Add(probe.PostalCode!.Trim());

        if (!string.IsNullOrWhiteSpace(probe.City))
            parts.Add(probe.City!.Trim());

        return string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static OrganizationCandidate MapCandidate(ReSearchResult item)
    {
        // Construction d’une adresse simple à partir du siège
        string? addressLine1 = null;
        string? postalCode = null;
        string? city = null;

        if (item.Siege is not null)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(item.Siege.NumeroVoie))
                sb.Append(item.Siege.NumeroVoie.Trim()).Append(' ');

            if (!string.IsNullOrWhiteSpace(item.Siege.TypeVoie))
                sb.Append(item.Siege.TypeVoie.Trim()).Append(' ');

            if (!string.IsNullOrWhiteSpace(item.Siege.LibelleVoie))
                sb.Append(item.Siege.LibelleVoie.Trim());

            addressLine1 = sb.ToString().Trim();
            postalCode = item.Siege.CodePostal;
            city = item.Siege.LibelleCommune;
        }

        return new OrganizationCandidate
        {
            Source = "RechercheEntreprises",
            Siren = item.Siren,
            Siret = item.Siege?.Siret,
            OfficialName = item.NomComplet ?? item.NomRaisonSociale,
            TradeName = item.NomCommercial,
            AddressLine1 = string.IsNullOrWhiteSpace(addressLine1) ? null : addressLine1,
            AddressLine2 = null, // non fourni par l’API, on pourra l’enrichir plus tard si besoin
            PostalCode = postalCode,
            City = city,
            CountryCode = "FR",
            Score = item.Score,
            IsSelected = false // on ne marque pas explicitement, c’est BestMatch qui fait foi
        };
    }

    // --- DTO internes pour la désérialisation JSON ---

    private sealed class ReSearchResponse
    {
        [JsonPropertyName("total_results")]
        public int TotalResults { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }

        [JsonPropertyName("results")]
        public List<ReSearchResult> Results { get; set; } = new();
    }

    private sealed class ReSearchResult
    {
        [JsonPropertyName("siren")]
        public string? Siren { get; set; }

        [JsonPropertyName("nom_complet")]
        public string? NomComplet { get; set; }

        [JsonPropertyName("nom_raison_sociale")]
        public string? NomRaisonSociale { get; set; }

        [JsonPropertyName("nom_commercial")]
        public string? NomCommercial { get; set; }

        [JsonPropertyName("score")]
        public double? Score { get; set; }

        [JsonPropertyName("siege")]
        public ReSiege? Siege { get; set; }
    }

    private sealed class ReSiege
    {
        [JsonPropertyName("siret")]
        public string? Siret { get; set; }

        [JsonPropertyName("numero_voie")]
        public string? NumeroVoie { get; set; }

        [JsonPropertyName("type_voie")]
        public string? TypeVoie { get; set; }

        [JsonPropertyName("libelle_voie")]
        public string? LibelleVoie { get; set; }

        [JsonPropertyName("code_postal")]
        public string? CodePostal { get; set; }

        [JsonPropertyName("libelle_commune")]
        public string? LibelleCommune { get; set; }
    }
}
