namespace SharpPyxis.LocalServices.Api.Models;

/// <summary>
/// Describes the basic metadata extracted from an image payload.
/// </summary>
public class ImageInfo
{
    /// <summary>
    /// Gets or sets the image width in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the image height in pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the vertical resolution in DPI.
    /// </summary>
    public float VerticalResolution { get; set; }

    /// <summary>
    /// Gets or sets the horizontal resolution in DPI.
    /// </summary>
    public float HorizontalResolution { get; set; }

    /// <summary>
    /// Gets or sets the physical width reported by the image metadata.
    /// </summary>
    public float PhysicalWidth { get; set; }

    /// <summary>
    /// Gets or sets the physical height reported by the image metadata.
    /// </summary>
    public float PhysicalHeight { get; set; }

    /// <summary>
    /// Gets or sets the detected image format extension.
    /// </summary>
    public string Format { get; set; } = string.Empty;
}
