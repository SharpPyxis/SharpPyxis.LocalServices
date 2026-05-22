namespace SharpPyxis.LocalServices.Api.Services;

/// <summary>
/// Maps file names or extensions to MIME types.
/// </summary>
public interface IMimeMappingService
{
    /// <summary>
    /// Resolves the MIME type for the supplied file name.
    /// </summary>
    string Map(string fileName);
}
