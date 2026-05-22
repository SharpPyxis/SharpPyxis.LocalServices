using Microsoft.AspNetCore.Mvc;
using SharpPyxis.LocalServices.Api.Models;
using SharpPyxis.LocalServices.Api.Services;

namespace SharpPyxis.LocalServices.Api.Controllers;

/// <summary>
/// Exposes organization data-quality operations backed by external reference registries.
/// </summary>
public class DataQualityOrganizationsController : ControllerBase
{
    private readonly IOrganizationReferenceService _referenceService;

    /// <summary>
    /// Initializes the controller with the organization reference service.
    /// </summary>
    public DataQualityOrganizationsController(IOrganizationReferenceService referenceService)
    {
        _referenceService = referenceService;
    }

    /// <summary>
    /// Normalizes organization data by querying the configured reference service.
    /// </summary>
    [HttpPost("data-quality/organizations/normalize")]
    [ProducesResponseType(typeof(OrganizationNormalizeResult), 200)]
    public async Task<ActionResult<OrganizationNormalizeResult>> NormalizeOrganization(
        [FromBody] OrganizationProbe probe,
        CancellationToken cancellationToken)
    {
        if (probe is null)
        {
            return BadRequest("Request body is required.");
        }

        var result = await _referenceService.NormalizeAsync(probe, cancellationToken);
        return Ok(result);
    }
}
