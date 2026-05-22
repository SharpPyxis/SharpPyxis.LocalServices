namespace SharpPyxis.LocalServices.Api.Models;

/// <summary>
/// Données actuelles du tiers telles qu'elles existent dans l'ERP.
/// Ce modèle est volontairement simple et "ERP-friendly".
/// </summary>
public class OrganizationProbe
{
    /// <summary>
    /// Identifiant interne de l'ERP.
    /// </summary>
    public string? ErpId { get; set; }

    /// <summary>
    /// SIREN (9 chiffres, sans espaces).
    /// </summary>
    public string? Siren { get; set; }

    /// <summary>
    /// SIRET (14 chiffres, sans espaces).
    /// </summary>
    public string? Siret { get; set; }

    /// <summary>
    /// Raison sociale ou nom officiel saisi dans l'ERP.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Nom commercial éventuel.
    /// </summary>
    public string? TradeName { get; set; }

    /// <summary>
    /// Ligne d'adresse principale.
    /// </summary>
    public string? AddressLine1 { get; set; }

    /// <summary>
    /// Ligne d'adresse complémentaire.
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Code postal.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Ville.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Code pays (ISO 3166-1 alpha-2 idéalement, ex : "FR").
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Champs divers de l'ERP qui peuvent aider au matching.
    /// </summary>
    public Dictionary<string, string?> Extra { get; set; } = new();
}
