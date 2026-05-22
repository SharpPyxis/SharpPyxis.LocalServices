using SharpPyxis.LocalServices.Api.Models;

namespace SharpPyxis.LocalServices.Api.Services;

/// <summary>
/// Provides organization normalization against an external reference source.
/// </summary>
public interface IOrganizationReferenceService
{
    /// <summary>
    /// Normalizes the supplied organization probe.
    /// </summary>
    Task<OrganizationNormalizeResult> NormalizeAsync(OrganizationProbe probe, CancellationToken cancellationToken = default);
}
