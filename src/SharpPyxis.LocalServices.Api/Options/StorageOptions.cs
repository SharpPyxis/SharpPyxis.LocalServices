// Options/StorageOptions.cs
namespace SharpPyxis.LocalServices.Api.Options;

/// <summary>
/// Configures local temporary storage used by the API.
/// </summary>
public class StorageOptions
{
    /// <summary>
    /// Gets or sets the working directory for temporary files.
    /// When not set, the API falls back to {ContentRoot}/temp.
    /// </summary>
    public string? WorkDir { get; set; }
}
