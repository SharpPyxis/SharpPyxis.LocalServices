using Microsoft.AspNetCore.StaticFiles;

namespace SharpPyxis.LocalServices.Api.Services;

/// <summary>
/// Default MIME mapping implementation based on file extensions.
/// </summary>
public class MimeMappingService : IMimeMappingService
{
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    /// <summary>
    /// Initializes the MIME mapping service.
    /// </summary>
    public MimeMappingService(FileExtensionContentTypeProvider contentTypeProvider)
    {
        _contentTypeProvider = contentTypeProvider;
    }

    /// <summary>
    /// Resolves the MIME type for a file name.
    /// </summary>
    public string Map(string fileName)
    {
        if (!_contentTypeProvider.TryGetContentType(fileName, out string? contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }
}
