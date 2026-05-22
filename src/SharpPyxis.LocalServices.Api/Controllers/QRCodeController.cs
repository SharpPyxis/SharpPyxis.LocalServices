using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Drawing;
using System.Text.RegularExpressions;

namespace SharpPyxis.LocalServices.Api.Controllers;

/// <summary>
/// Generates QR codes that can be consumed from lightweight local-service workflows.
/// </summary>
[ApiController]
public class QRCodeController : ControllerBase
{
    /// <summary>
    /// Génère un QR code. 
    /// Format par défaut : PNG. Paramètres courts pour intégration SQL.
    /// </summary>
    /// <param name="text">Contenu à encoder (alias: t)</param>
    /// <param name="eccLevel">Niveau ECC L/M/Q/H (alias: l)</param>
    /// <param name="darkCssColor">Couleur foncée hex sans # (ex: 000000) (alias: dc)</param>
    /// <param name="lightCssColor">Couleur claire hex sans # (ex: ffffff) (alias: lc)</param>
    /// <param name="moduleSize">Taille d’un module en pixels (alias: ms)</param>
    /// <param name="fmt">png|bmp (par défaut: png)</param>
    [HttpGet("qr-code")]
    [Produces("image/png", "image/bmp", "application/problem+json")]
    public IActionResult Encode(
        [FromQuery(Name = "t")] string? text,
        [FromQuery(Name = "l")] string? eccLevel = "Q",
        [FromQuery(Name = "dc")] string? darkCssColor = "000000",
        [FromQuery(Name = "lc")] string? lightCssColor = "ffffff",
        [FromQuery(Name = "ms")] int moduleSize = 5,
        [FromQuery(Name = "fmt")] string? fmt = "png")
    {
        // --- validations d’entrée ---
        if (string.IsNullOrWhiteSpace(text))
            return Problem(title: "Paramètre manquant", detail: "Le paramètre 't' (texte) est requis.", statusCode: StatusCodes.Status400BadRequest);

        if (moduleSize < 1 || moduleSize > 50)
            return Problem(title: "Paramètre invalide", detail: "Le paramètre 'ms' doit être entre 1 et 50.", statusCode: StatusCodes.Status400BadRequest);

        if (!TryNormalizeHex(darkCssColor, out var darkHex)
            || !TryNormalizeHex(lightCssColor, out var lightHex))
            return Problem(title: "Couleur invalide", detail: "Paramètres 'dc' et 'lc' doivent être des hex RGB (3 ou 6 caractères, sans #).", statusCode: StatusCodes.Status400BadRequest);

        Color dark = ColorTranslator.FromHtml(darkHex);
        Color light = ColorTranslator.FromHtml(lightHex);

        var level = MapEcc(eccLevel);

        // --- génération (QRCoder) ---
        // On privilégie PNG pour compacité ; BMP reste disponible pour compat.
        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(text, level);

            if (fmt == "bmp")
            {
                using var bmpCode = new BitmapByteQRCode(data);
                var bytes = bmpCode.GetGraphic(moduleSize, darkHex, lightHex);
                return File(bytes, "image/bmp");
            }
            else // png (default)
            {
                // If your QRCoder version provides PngByteQRCode with Color overload:
                using var pngCode = new PngByteQRCode(data);
                var bytes = pngCode.GetGraphic(moduleSize, dark, light, drawQuietZones: true);
                return File(bytes, "image/png");
            }
        }
        catch (Exception ex)
        {
            // Sortie homogène en ProblemDetails
            return Problem(
                title: "Échec de la génération du QR code",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    // --- helpers ---

    // Accepte "q", "Q", "l", "m", "h" ; défaut : Q
    private static QRCodeGenerator.ECCLevel MapEcc(string? ecc) =>
        (ecc ?? "Q").Trim().ToUpperInvariant() switch
        {
            "L" => QRCodeGenerator.ECCLevel.L,
            "M" => QRCodeGenerator.ECCLevel.M,
            "H" => QRCodeGenerator.ECCLevel.H,
            _ => QRCodeGenerator.ECCLevel.Q
        };

    // Normalise un hex CSS sans '#' vers "#RRGGBB" ; accepte 3 ou 6 hexdigits
    private static bool TryNormalizeHex(string? input, out string hexWithHash)
    {
        hexWithHash = "#000000";
        if (string.IsNullOrWhiteSpace(input)) return false;

        var s = input.Trim().TrimStart('#');
        if (!Regex.IsMatch(s, "^[0-9a-fA-F]{3}([0-9a-fA-F]{3})?$"))
            return false;

        if (s.Length == 3)
        {
            // ABC -> AABBCC
            s = string.Concat(s[0], s[0], s[1], s[1], s[2], s[2]);
        }

        hexWithHash = "#" + s.ToLowerInvariant();
        return true;
    }
}
