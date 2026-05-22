using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace SharpPyxis.LocalServices.Api.Controllers;

/// <summary>
/// Utilitaires de sécurité : chaînes aléatoires, hachages SHA-2, génération de mots de passe.
/// </summary>
[ApiController]
[Route("security")]
public class SecurityController : ControllerBase
{
    // ---------------- RND STRING ----------------

    /// <summary>Génère une chaîne aléatoire à partir d’un alphabet donné.</summary>
    /// <remarks>
    /// Appel : <c>GET /security/rndstring?len=12&amp;chars=ABC...</c><br/>
    /// Entropie cryptographique (CSPRNG).
    /// </remarks>
    /// <param name="len">Longueur (1..128, défaut 12).</param>
    /// <param name="chars">Alphabet (min 2 chars, défaut chiffres+lettres sans I/O/l).</param>
    [HttpGet("rndstring")]
    [Produces("text/plain", "application/problem+json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult RndString(
        [FromQuery] int len = 12,
        [FromQuery] string? chars = null)
    {
        if (len < 1 || len > 128)
            return Problem(title: "Paramètre invalide", detail: "len doit être entre 1 et 128.", statusCode: 400);

        var alphabet = string.IsNullOrWhiteSpace(chars)
            ? "0123456789abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ"
            : chars.Trim();

        if (alphabet.Length < 2)
            return Problem(title: "Alphabet invalide", detail: "chars doit contenir au moins 2 caractères distincts.", statusCode: 400);

        var sb = new StringBuilder(len);
        for (int i = 0; i < len; i++)
        {
            int idx = RandomNumberGenerator.GetInt32(alphabet.Length);
            sb.Append(alphabet[idx]);
        }
        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }

    // ---------------- HASH SHA-256 / SHA-512 ----------------

    /// <summary>
    /// Calcule le hash SHA-256 du corps de la requête (binaire ou texte). Résultat en Base64.
    /// </summary>
    /// <remarks>
    /// Envoyez le contenu à hacher dans le corps en <c>application/octet-stream</c> (ou <c>text/plain</c>/<c>application/pdf</c>/…).
    /// </remarks>
    [HttpPost("sha256")]
    [Produces("text/plain", "application/problem+json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Sha256()
    {
        using var sha = SHA256.Create();
        // hash direct sur le flux d’entrée (pas de buffering inutile)
        byte[] hash = await Task.Run(() => sha.ComputeHash(Request.Body));
        return Content(Convert.ToBase64String(hash), "text/plain", Encoding.UTF8);
    }

    /// <summary>
    /// Calcule le hash SHA-512 du corps de la requête (binaire ou texte). Résultat en Base64.
    /// </summary>
    [HttpPost("sha512")]
    [Produces("text/plain", "application/problem+json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Sha512()
    {
        using var sha = SHA512.Create();
        byte[] hash = await Task.Run(() => sha.ComputeHash(Request.Body));
        return Content(Convert.ToBase64String(hash), "text/plain", Encoding.UTF8);
    }

    // ---------------- GENERATE PASSWORD ----------------

    /// <summary>
    /// Propose un mot de passe conforme aux contraintes (maj/min/chiffre/symbole, longueur, unicité).
    /// </summary>
    /// <param name="requireUppercase">Au moins une majuscule.</param>
    /// <param name="requireLowercase">Au moins une minuscule.</param>
    /// <param name="requireDigit">Au moins un chiffre.</param>
    /// <param name="requireNonAlphanumeric">Au moins un symbole.</param>
    /// <param name="requiredLength">Longueur minimale (6..128, défaut 12).</param>
    /// <param name="requiredUniqueChars">Nombre minimal de caractères distincts (1..len, défaut 8).</param>
    [HttpGet("generate-password")]
    [Produces("text/plain", "application/problem+json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult GeneratePassword(
        [FromQuery(Name = "upper")] bool requireUppercase = true,
        [FromQuery(Name = "lower")] bool requireLowercase = true,
        [FromQuery(Name = "digit")] bool requireDigit = true,
        [FromQuery(Name = "nonalpha")] bool requireNonAlphanumeric = true,
        [FromQuery(Name = "len")] int requiredLength = 12,
        [FromQuery(Name = "uniquechars")] int requiredUniqueChars = 8)
    {
        if (requiredLength < 6 || requiredLength > 128)
            return Problem(title: "Paramètre invalide", detail: "len doit être entre 6 et 128.", statusCode: 400);
        if (requiredUniqueChars < 1 || requiredUniqueChars > requiredLength)
            return Problem(title: "Paramètre invalide", detail: "uniquechars doit être entre 1 et len.", statusCode: 400);

        const string U = "ABCDEFGHJKLMNPQRSTUVWXYZ";  // sans I/O
        const string L = "abcdefghijkmnopqrstuvwxyz"; // sans l/o
        const string D = "0123456789";
        const string S = "!@$?%*€#-_+.:,;";

        var pools = new List<string>(4);
        if (requireUppercase) pools.Add(U);
        if (requireLowercase) pools.Add(L);
        if (requireDigit) pools.Add(D);
        if (requireNonAlphanumeric) pools.Add(S);

        string all = (pools.Count > 0) ? string.Concat(pools) : (U + L + D + S);

        var chars = new List<char>(requiredLength);

        // Garantir chaque contrainte une fois
        foreach (var pool in pools)
            chars.Add(pool[RandomNumberGenerator.GetInt32(pool.Length)]);

        // Compléter jusqu'à la longueur
        while (chars.Count < requiredLength)
            chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

        // Mélange Fisher–Yates (CSPRNG)
        for (int i = chars.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        // Satisfaire uniquechars si besoin
        var distinct = chars.Distinct().Count();
        if (distinct < requiredUniqueChars)
        {
            var need = requiredUniqueChars - distinct;
            var allSet = new HashSet<char>(all);
            var used = new HashSet<char>(chars);
            var missing = allSet.Except(used).ToArray();

            for (int k = 0; k < need && k < missing.Length; k++)
            {
                int pos = RandomNumberGenerator.GetInt32(chars.Count);
                chars[pos] = missing[k];
            }
        }

        return Content(new string(chars.ToArray()), "text/plain", Encoding.UTF8);
    }
}
