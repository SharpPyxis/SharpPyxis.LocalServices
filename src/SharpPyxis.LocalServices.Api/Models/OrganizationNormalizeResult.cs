namespace SharpPyxis.LocalServices.Api.Models;

/// <summary>
/// Résultat de la normalisation d'un tiers via des registres.
/// </summary>
public class OrganizationNormalizeResult
{
    /// <summary>
    /// Données d'origine envoyées par l'ERP.
    /// </summary>
    public OrganizationProbe Probe { get; set; } = new();

    /// <summary>
    /// Statut du matching : "NotProcessed", "Exact", "Single", "Multiple", "None", "Error"…
    /// </summary>
    public string Status { get; set; } = "NotProcessed";

    /// <summary>
    /// Message technique ou fonctionnel (ex : détail d'erreur, info sur la recherche…).
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Candidat recommandé (s'il y en a un).
    /// </summary>
    public OrganizationCandidate? BestMatch { get; set; }

    /// <summary>
    /// Liste complète des candidats retournés.
    /// </summary>
    public List<OrganizationCandidate> Candidates { get; set; } = new();

    /// <summary>
    /// Timestamp de traitement (UTC).
    /// </summary>
    public DateTimeOffset ProcessedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
