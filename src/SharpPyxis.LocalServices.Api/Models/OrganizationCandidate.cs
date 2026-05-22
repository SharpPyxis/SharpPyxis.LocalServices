namespace SharpPyxis.LocalServices.Api.Models;

/// <summary>
/// Candidat retourné par un registre (SIRENE, autre…).
/// </summary>
public class OrganizationCandidate
{
    /// <summary>
    /// Source : par ex. "SIRENE", "RNCS", "Mock"…
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// SIREN officiel (si disponible).
    /// </summary>
    public string? Siren { get; set; }

    /// <summary>
    /// SIRET officiel (si disponible).
    /// </summary>
    public string? Siret { get; set; }

    /// <summary>
    /// Dénomination officielle (raison sociale).
    /// </summary>
    public string? OfficialName { get; set; }

    /// <summary>
    /// Nom commercial éventuel.
    /// </summary>
    public string? TradeName { get; set; }

    /// <summary>
    /// Gets or sets the first address line.
    /// </summary>
    public string? AddressLine1 { get; set; }

    /// <summary>
    /// Gets or sets the second address line.
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the postal code.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Gets or sets the ISO country code.
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Score de similarité (0.0 - 1.0) si calculé.
    /// </summary>
    public double? Score { get; set; }

    /// <summary>
    /// Le candidat recommandé (ex: meilleur score).
    /// </summary>
    public bool IsSelected { get; set; }
}
