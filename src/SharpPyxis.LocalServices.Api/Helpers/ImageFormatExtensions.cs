using System.Drawing.Imaging;

namespace SharpPyxis.LocalServices.Api.Helpers;

/// <summary>
/// Converts between file extensions and <see cref="ImageFormat"/> values.
/// </summary>
public static class ImageFormatExtensions
{
    /// <summary>
    /// Resolves an <see cref="ImageFormat"/> from a file extension.
    /// </summary>
    public static ImageFormat FromExtension(string ext)
    {
        return ext.ToUpper() switch
        {
            "BMP" => ImageFormat.Bmp,
            "EMF" => ImageFormat.Emf,
            "EXIF" => ImageFormat.Exif,
            "GIF" => ImageFormat.Gif,
            "ICON" => ImageFormat.Icon,
            "JPEG" => ImageFormat.Jpeg,
            "PNG" => ImageFormat.Png,
            "TIFF" => ImageFormat.Tiff,
            "WMF" => ImageFormat.Wmf,
            _ => ImageFormat.Bmp,
        };
    }

    /// <summary>
    /// Returns the conventional file extension for an <see cref="ImageFormat"/>.
    /// </summary>
    public static string GetExtension(this ImageFormat rawFormat)
    {
        if (rawFormat.Equals(ImageFormat.Bmp))
            return "Bmp";
        else if (rawFormat.Equals(ImageFormat.Emf))
            return "Emf";
        else if (rawFormat.Equals(ImageFormat.Exif))
            return "Exif";
        else if (rawFormat.Equals(ImageFormat.Gif))
            return "Gif";
        else if (rawFormat.Equals(ImageFormat.Icon))
            return "Ico";
        else if (rawFormat.Equals(ImageFormat.Jpeg))
            return "Jpg";
        else if (rawFormat.Equals(ImageFormat.Png))
            return "Png";
        else if (rawFormat.Equals(ImageFormat.Tiff))
            return "Tiff";
        else if (rawFormat.Equals(ImageFormat.Wmf))
            return "Wmf";
        return string.Empty;
    }
}
