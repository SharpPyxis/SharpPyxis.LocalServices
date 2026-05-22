using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using SharpPyxis.LocalServices.Api.Helpers;
using SharpPyxis.LocalServices.Api.Models;
using SharpPyxis.LocalServices.Api.Services;

namespace SharpPyxis.LocalServices.Api.Controllers;

/// <summary>
/// Utilitaires d’images (mimetype, informations, redimensionnement, fit, rotation).
/// </summary>
[ApiController]
[Route("drawing")]
public sealed class DrawingController(IMimeMappingService mimeMappingService) : ControllerBase
{

    // ---------------- MIME TYPE ----------------

    /// <summary>Retourne le type MIME correspondant au nom/extension fourni.</summary>
    /// <param name="name">Nom de fichier (ou “.ext”).</param>
    [HttpGet("mimetype")]
    [Produces("text/plain", "application/problem+json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult GetMimeType([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Problem(title: "Paramètre manquant", detail: "Le paramètre 'name' est requis.", statusCode: 400);

        return Content(mimeMappingService.Map(name), "text/plain");
    }

    // ---------------- IMAGE INFO ----------------

    /// <summary>Extrait les méta-données d’une image postée en binaire dans le corps.</summary>
    [HttpPost("image-info")]
    [Produces("application/json", "application/problem+json")]
    [ProducesResponseType(typeof(ImageInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult GetImageInfo()
    {
        try
        {
            using var bmp = new Bitmap(Request.Body);
            var info = new ImageInfo
            {
                Width = bmp.Width,
                Height = bmp.Height,
                VerticalResolution = bmp.VerticalResolution,
                HorizontalResolution = bmp.HorizontalResolution,
                PhysicalWidth = bmp.PhysicalDimension.Width,
                PhysicalHeight = bmp.PhysicalDimension.Height,
                Format = bmp.RawFormat.GetExtension().ToUpperInvariant()
            };
            return Ok(info);
        }
        catch (Exception ex)
        {
            return Problem(title: "Image invalide", detail: ex.Message, statusCode: 400);
        }
    }

    // ---------------- RESIZE ----------------

    /// <summary>
    /// Redimensionne une image (homothétique si une seule dimension est fournie).
    /// </summary>
    /// <param name="width">Largeur cible (optionnel).</param>
    /// <param name="height">Hauteur cible (optionnel).</param>
    /// <param name="ext">Extension de sortie (ex: png, jpg…). Si omise, conserve le format d’origine.</param>
    [HttpPost("resize")]
    [Produces("image/bmp", "image/png", "image/jpeg", "image/gif", "image/tiff", "image/x-icon", "image/wmf", "application/problem+json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Resize(
        [FromQuery(Name = "nw")] int? width,
        [FromQuery(Name = "nh")] int? height,
        [FromQuery(Name = "ext")] string? ext)
    {
        if (width is null && height is null)
            return Problem(title: "Paramètres manquants", detail: "Fournir au moins 'nw' ou 'nh'.", statusCode: 400);

        if (width is < 1 || height is < 1)
            return Problem(title: "Paramètres invalides", detail: "nw/nh doivent être >= 1.", statusCode: 400);

        try
        {
            using var source = new Bitmap(Request.Body);

            // Taille cible homothétique
            int newW, newH;
            if (width.HasValue && !height.HasValue)
            {
                newW = Math.Min(width.Value, source.Width);
                newH = (int)Math.Round(source.Height * (newW / (double)source.Width));
            }
            else if (!width.HasValue && height.HasValue)
            {
                newH = Math.Min(height.Value, source.Height);
                newW = (int)Math.Round(source.Width * (newH / (double)source.Height));
            }
            else
            {
                // Les deux précisés : on accepte la déformation (comportement historique)
                newW = Math.Min(width!.Value, source.Width);
                newH = Math.Min(height!.Value, source.Height);
            }

            using var target = new Bitmap(newW, newH, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(target))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(source, new Rectangle(0, 0, newW, newH));
            }

            var (fmt, mime) = ResolveFormat(ext, source.RawFormat);
            using var ms = new MemoryStream();
            SaveWithFormat(target, ms, fmt);
            ms.Position = 0;
            return File(ms.ToArray(), mime);
        }
        catch (Exception ex)
        {
            return Problem(title: "Échec du redimensionnement", detail: ex.Message, statusCode: 400);
        }
    }

    // ---------------- FIT (LETTERBOX) ----------------

    /// <summary>
    /// Ajuste l’image dans un canevas <c>w×h</c> sans recadrage (contain), centrée.
    /// Supporte les fonds transparents si le format de sortie le permet.
    /// </summary>
    /// <param name="width">Largeur du canevas (≥1).</param>
    /// <param name="height">Hauteur du canevas (≥1).</param>
    /// <param name="ext">Extension de sortie (png, jpg, tiff, gif, ico…). Si omise, conserve le format d’origine (ou bascule en PNG si transparence demandée).</param>
    /// <param name="bg">
    /// Couleur de fond :
    /// - <c>transparent</c> pour fond entièrement transparent
    /// - hex sans # : <c>RRGGBB</c> (opaque) ou <c>RRGGBBAA</c> (AA = alpha, 00 transparent … FF opaque)  
    /// Défaut : <c>FFFFFF</c> (blanc opaque).
    /// </param>
    [HttpPost("fit")]
    [Produces("image/bmp", "image/png", "image/jpeg", "image/gif", "image/tiff", "image/x-icon", "image/wmf", "application/problem+json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Fit(
        [FromQuery(Name = "w")] int width,
        [FromQuery(Name = "h")] int height,
        [FromQuery(Name = "ext")] string? ext,
        [FromQuery(Name = "bg")] string? bg = null)
    {
        if (width < 1 || height < 1)
            return Problem(title: "Paramètres invalides", detail: "w et h doivent être ≥ 1.", statusCode: 400);

        try
        {
            using var source = new Bitmap(Request.Body);

            // Échelle homothétique “contain”
            double scale = Math.Min(width / (double)source.Width, height / (double)source.Height);
            if (scale <= 0) scale = 1;

            int scaledW = Math.Max(1, (int)Math.Round(source.Width * scale));
            int scaledH = Math.Max(1, (int)Math.Round(source.Height * scale));
            int offsetX = (width - scaledW) / 2;
            int offsetY = (height - scaledH) / 2;

            // Couleur de fond (peut être transparente)
            var bgColor = ParseHtmlColorOrTransparent(bg) ?? Color.White;
            bool wantsTransparency = bgColor.A < 255 || string.Equals(bg, "transparent", StringComparison.OrdinalIgnoreCase);

            // Résolution du format cible (avec logique alpha)
            var (fmt, mime, fmtFromExt) = ResolveFormatWithAlpha(ext, source.RawFormat);

            // Transparence demandée mais format imposé non compatible → 400
            if (wantsTransparency && fmtFromExt && !SupportsAlpha(fmt))
            {
                return Problem(
                    title: "Format sans transparence",
                    detail: "Le format demandé ne supporte pas la transparence. Utilisez png/tiff/gif/ico ou omettez 'ext'.",
                    statusCode: 400);
            }

            // Transparence demandée et format non imposé, ou format origine non alpha → bascule auto PNG
            if (wantsTransparency && !fmtFromExt && !SupportsAlpha(fmt))
            {
                fmt = ImageFormat.Png;
                mime = "image/png";
            }

            using var target = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(target))
            {
                g.Clear(bgColor); // si A < 255 → canevas transparent
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(source, new Rectangle(offsetX, offsetY, scaledW, scaledH));
            }

            using var ms = new MemoryStream();
            SaveWithFormat(target, ms, fmt);
            ms.Position = 0;
            return File(ms.ToArray(), mime);
        }
        catch (Exception ex)
        {
            return Problem(title: "Échec du fit", detail: ex.Message, statusCode: 400);
        }
    }


    // ---------------- ROTATE ----------------

    /// <summary>
    /// Pivote une image de 0/90/180/270 degrés (sens horaire).
    /// </summary>
    /// <param name="angle">Angle de rotation autorisé : 0, 90, 180, 270.</param>
    /// <param name="ext">Extension de sortie (ex: png, jpg…). Si omise, conserve le format d’origine.</param>
    [HttpPost("rotate")]
    [Produces("image/bmp", "image/png", "image/jpeg", "image/gif", "image/tiff", "image/x-icon", "image/wmf", "application/problem+json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Rotate(
        [FromQuery(Name = "a")] int angle,
        [FromQuery(Name = "ext")] string? ext)
    {
        if (angle is not (0 or 90 or 180 or 270))
            return Problem(title: "Paramètre invalide", detail: "a doit être 0, 90, 180 ou 270.", statusCode: 400);

        try
        {
            using var bmp = new Bitmap(Request.Body);
            if (angle == 90) bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
            else if (angle == 180) bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
            else if (angle == 270) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
            // angle == 0 => no-op

            var (fmt, mime) = ResolveFormat(ext, bmp.RawFormat);
            using var ms = new MemoryStream();
            SaveWithFormat(bmp, ms, fmt);
            ms.Position = 0;
            return File(ms.ToArray(), mime);
        }
        catch (Exception ex)
        {
            return Problem(title: "Échec de la rotation", detail: ex.Message, statusCode: 400);
        }
    }

    // ---------------- Helpers ----------------

    private static (ImageFormat Format, string Mime) ResolveFormat(string? ext, ImageFormat fallbackRaw)
    {
        // Conserver le format d’origine si ext absent
        if (string.IsNullOrWhiteSpace(ext))
        {
            var fallbackExt = fallbackRaw.GetExtension();
            return (fallbackRaw, MimeFromExt(fallbackExt));
        }

        var f = ext.Trim().TrimStart('.').ToLowerInvariant();

        // Utilise ton helper ImageFormatExtensions si dispo
        ImageFormat format = f switch
        {
            "bmp" => ImageFormat.Bmp,
            "png" => ImageFormat.Png,
            "jpg" or "jpeg" => ImageFormat.Jpeg,
            "gif" => ImageFormat.Gif,
            "tif" or "tiff" => ImageFormat.Tiff,
            "ico" => ImageFormat.Icon,
            "wmf" => ImageFormat.Wmf,
            "emf" => ImageFormat.Emf,
            "exif" => ImageFormat.Exif,
            _ => fallbackRaw
        };

        var mime = MimeFromExt(format.GetExtension());
        return (format, mime);
    }

    private static string MimeFromExt(string extNoDot)
        => extNoDot.StartsWith(".") ? System.Net.Mime.MediaTypeNames.Application.Octet : extNoDot switch
        {
            "bmp" => "image/bmp",
            "png" => "image/png",
            "jpg" or "jpeg" => "image/jpeg",
            "gif" => "image/gif",
            "tiff" or "tif" => "image/tiff",
            "ico" => "image/x-icon",
            "wmf" => "image/wmf",
            "emf" => "image/emf",
            "exif" => "image/jpeg", // EXIF stocké généralement en JPEG
            _ => "application/octet-stream"
        };

    private static void SaveWithFormat(Image img, Stream output, ImageFormat format)
    {
        // Pour JPEG : encoder qualité raisonnable (si besoin)
        if (format.Guid == ImageFormat.Jpeg.Guid)
        {
            var codec = ImageCodecInfo.GetImageDecoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
            if (codec is null) { img.Save(output, format); return; }

            using var encParams = new EncoderParameters(1);
            encParams.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
            img.Save(output, codec, encParams);
            return;
        }
        img.Save(output, format);
    }


    private static (ImageFormat Format, string Mime, bool FormatCameFromExt) ResolveFormatWithAlpha(string? ext, ImageFormat fallbackRaw)
    {
        if (string.IsNullOrWhiteSpace(ext))
        {
            var fallbackExt = fallbackRaw.GetExtension();
            return (fallbackRaw, MimeFromExt(fallbackExt), false);
        }

        var f = ext.Trim().TrimStart('.').ToLowerInvariant();
        ImageFormat format = f switch
        {
            "bmp" => ImageFormat.Bmp,
            "png" => ImageFormat.Png,
            "jpg" or "jpeg" => ImageFormat.Jpeg,
            "gif" => ImageFormat.Gif,
            "tif" or "tiff" => ImageFormat.Tiff,
            "ico" => ImageFormat.Icon,
            "wmf" => ImageFormat.Wmf,
            "emf" => ImageFormat.Emf,
            "exif" => ImageFormat.Exif,
            _ => fallbackRaw
        };
        var mime = MimeFromExt(format.GetExtension());
        return (format, mime, true);
    }

    private static bool SupportsAlpha(ImageFormat format)
    {
        // Alpha “fiable” côté formats raster courants
        if (format.Guid == ImageFormat.Png.Guid) return true;
        if (format.Guid == ImageFormat.Tiff.Guid) return true;
        if (format.Guid == ImageFormat.Gif.Guid) return true; // 1-bit transparency
        if (format.Guid == ImageFormat.Icon.Guid) return true;

        // BMP 32bpp peut contenir de l’alpha mais son interprétation est variable selon les viewers → on évite
        return false;
    }

    private static Color? ParseHtmlColorOrTransparent(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (string.Equals(s.Trim(), "transparent", StringComparison.OrdinalIgnoreCase))
            return Color.FromArgb(0, 0, 0, 0);

        // RRGGBB ou RRGGBBAA
        s = s.Trim().TrimStart('#');
        if (s.Length is 6 or 8 && s.All(Uri.IsHexDigit))
        {
            byte r = Convert.ToByte(s.Substring(0, 2), 16);
            byte g = Convert.ToByte(s.Substring(2, 2), 16);
            byte b = Convert.ToByte(s.Substring(4, 2), 16);

            if (s.Length == 6) return Color.FromArgb(255, r, g, b);

            // AA = alpha (00 transparent … FF opaque) en fin : RRGGBBAA
            byte a = Convert.ToByte(s.Substring(6, 2), 16);
            return Color.FromArgb(a, r, g, b);
        }
        return null;
    }


}
