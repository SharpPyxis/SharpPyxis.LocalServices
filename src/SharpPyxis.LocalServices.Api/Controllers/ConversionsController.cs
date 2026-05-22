using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace SharpPyxis.LocalServices.Api.Controllers;

/// <summary>
/// Conversions binaires usuelles : base64 ↔ binaire, hex ↔ binaire.
/// <para>
/// Les opérations privilégient la lecture en flux (stream-first) pour limiter la pression mémoire.
/// </para>
/// </summary>
[ApiController]
[Route("conversions")]
public class ConversionsController : ControllerBase
{
    /// <summary>
    /// Convertit une chaîne base64 en binaire.
    /// </summary>
    /// <remarks>
    /// <b>Entrée</b> : corps <c>text/plain</c> contenant une chaîne base64 valide (sans retours à la ligne).<br/>
    /// <b>Sortie</b> : <c>application/octet-stream</c> (octets décodés).<br/>
    /// </remarks>
    /// <response code="200">Flux binaire décodé.</response>
    /// <response code="400">Chaîne base64 absente ou invalide.</response>
    /// <response code="500">Erreur interne lors de la conversion.</response>
    [HttpPost("base64/binary")]
    [Consumes("text/plain", "application/json")]
    [Produces("application/octet-stream", "application/problem+json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Base64ToBinary(CancellationToken ct)
    {
        try
        {
            var b64 = await ReadBodyAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(b64))
                return Problem(title: "Corps manquant", detail: "Le corps doit contenir une chaîne base64.", statusCode: 400);

            try
            {
                var data = Convert.FromBase64String(b64.Trim());
                return File(data, "application/octet-stream");
            }
            catch (FormatException ex)
            {
                return Problem(title: "Base64 invalide", detail: ex.Message, statusCode: 400);
            }
        }
        catch (Exception ex)
        {
            return Problem(title: "Échec conversion base64 → binaire", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Convertit un flux binaire en base64.
    /// </summary>
    /// <remarks>
    /// <b>Entrée</b> : corps <c>application/octet-stream</c> (ou tout type binaire).<br/>
    /// <b>Sortie</b> : <c>text/plain</c> contenant la chaîne base64 (UTF-8, sans retours à la ligne).<br/>
    /// </remarks>
    /// <response code="200">Chaîne base64 encodée.</response>
    /// <response code="400">Corps binaire manquant.</response>
    /// <response code="500">Erreur interne lors de la conversion.</response>
    [HttpPost("binary/base64")]
    [Consumes("application/octet-stream", "application/pdf", "image/png", "image/bmp")]
    [Produces("text/plain", "application/problem+json")]
    [ProducesResponseType(typeof(ContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BinaryToBase64(CancellationToken ct)
    {
        try
        {
            var bytes = await ReadBodyAsBytesAsync(ct);
            if (bytes.Length == 0)
                return Problem(title: "Corps manquant", detail: "Le corps doit contenir des octets.", statusCode: 400);

            var b64 = Convert.ToBase64String(bytes);
            return Content(b64, "text/plain", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            return Problem(title: "Échec conversion binaire → base64", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Convertit une chaîne hexadécimale en binaire.
    /// </summary>
    /// <remarks>
    /// <b>Entrée</b> : corps <c>text/plain</c> contenant des digits hexadécimaux ; les séparateurs/espaces et le préfixe <c>0x</c> sont ignorés.<br/>
    /// <b>Sortie</b> : <c>application/octet-stream</c> (octets décodés).<br/>
    /// </remarks>
    /// <response code="200">Flux binaire décodé.</response>
    /// <response code="400">Chaîne hex absente ou invalide (nombre de digits impair, caractère non hex).</response>
    /// <response code="500">Erreur interne lors de la conversion.</response>
    [HttpPost("hex/binary")]
    [Consumes("text/plain", "application/json")]
    [Produces("application/octet-stream", "application/problem+json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HexToBinary(CancellationToken ct)
    {
        try
        {
            var hex = await ReadBodyAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(hex))
                return Problem(title: "Corps manquant", detail: "Le corps doit contenir une chaîne hexadécimale.", statusCode: 400);

            if (!TryDecodeHex(hex, out var bytes, out var error))
                return Problem(title: "Hex invalide", detail: error, statusCode: 400);

            return File(bytes, "application/octet-stream");
        }
        catch (Exception ex)
        {
            return Problem(title: "Échec conversion hex → binaire", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Convertit un flux binaire en chaîne hexadécimale compacte (minuscules).
    /// </summary>
    /// <remarks>
    /// <b>Entrée</b> : corps <c>application/octet-stream</c> (ou tout type binaire).<br/>
    /// <b>Sortie</b> : <c>text/plain</c> (hex sans espaces, en minuscules).<br/>
    /// </remarks>
    /// <response code="200">Chaîne hex compactée (lowercase).</response>
    /// <response code="400">Corps binaire manquant.</response>
    /// <response code="500">Erreur interne lors de la conversion.</response>
    [HttpPost("binary/hex")]
    [Consumes("application/octet-stream")]
    [Produces("text/plain", "application/problem+json")]
    [ProducesResponseType(typeof(ContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BinaryToHex(CancellationToken ct)
    {
        try
        {
            var bytes = await ReadBodyAsBytesAsync(ct);
            if (bytes.Length == 0)
                return Problem(title: "Corps manquant", detail: "Le corps doit contenir des octets.", statusCode: 400);

            var hex = EncodeHex(bytes);
            return Content(hex, "text/plain", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            return Problem(title: "Échec conversion binaire → hex", detail: ex.Message, statusCode: 500);
        }
    }

    // -------- helpers (non exposés) --------

    private async Task<string> ReadBodyAsStringAsync(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 8192, leaveOpen: false);
        return await reader.ReadToEndAsync(ct);
    }

    private async Task<byte[]> ReadBodyAsBytesAsync(CancellationToken ct)
    {
        var ms = Request.ContentLength is long len && len > 0
            ? new MemoryStream(checked((int)len))
            : new MemoryStream();

        await Request.Body.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    private static bool TryDecodeHex(string input, out byte[] bytes, out string error)
    {
        error = string.Empty;

        // Ne garder que les digits hex (ignore 0x, séparateurs, espaces…)
        var filtered = new string(input.Where(Uri.IsHexDigit).ToArray());
        if (filtered.Length % 2 != 0)
        {
            bytes = Array.Empty<byte>();
            error = "Nombre de digits hex impair.";
            return false;
        }

        try
        {
            var len = filtered.Length / 2;
            bytes = new byte[len];
            for (int i = 0; i < len; i++)
            {
                var hi = FromHex(filtered[2 * i]);
                var lo = FromHex(filtered[2 * i + 1]);
                if (hi < 0 || lo < 0)
                    throw new FormatException($"Caractère non hexadécimal à l’index {2 * i}.");
                bytes[i] = (byte)((hi << 4) | lo);
            }
            return true;
        }
        catch (Exception ex)
        {
            bytes = Array.Empty<byte>();
            error = ex.Message;
            return false;
        }

        static int FromHex(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            c = char.ToLowerInvariant(c);
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return -1;
        }
    }

    private static string EncodeHex(ReadOnlySpan<byte> bytes)
    {
        var table = "0123456789abcdef".AsSpan();
        var result = new char[bytes.Length * 2];
        var ri = 0;
        foreach (var b in bytes)
        {
            result[ri++] = table[(b >> 4) & 0xF];
            result[ri++] = table[b & 0xF];
        }
        return new string(result);
    }
}
