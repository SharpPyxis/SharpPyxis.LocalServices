using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Xml;

namespace SharpPyxis.LocalServices.Api.Controllers;

/// <summary>
/// Conversions de sérialisation entre JSON et XML.
/// <para>
/// Ces opérations utilisent <see cref="Newtonsoft.Json"/> pour la compatibilité maximale.
/// </para>
/// </summary>
[ApiController]
[Route("serialization")]
public class SerializationController : ControllerBase
{
    /// <summary>
    /// Convertit un flux JSON en XML.
    /// </summary>
    /// <remarks>
    /// <b>Entrée</b> : corps <c>application/json</c> ou <c>text/plain</c> contenant un objet JSON valide.<br/>
    /// <b>Sortie</b> : <c>application/xml</c> ou <c>text/xml</c> (UTF-8).<br/>
    /// Paramètre optionnel <c>?root=nom</c> pour définir l’élément racine.
    /// </remarks>
    /// <param name="rootElementName">Nom de l’élément racine à utiliser si absent (défaut : <c>root</c>).</param>
    /// <response code="200">XML généré avec succès.</response>
    /// <response code="400">Entrée JSON vide ou invalide.</response>
    /// <response code="500">Erreur interne lors de la conversion.</response>
    [HttpPost("json2xml")]
    [Consumes("application/json", "text/plain")]
    [Produces("application/xml", "application/problem+json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Json2Xml([FromQuery(Name = "root")] string? rootElementName)
    {
        rootElementName = string.IsNullOrWhiteSpace(rootElementName) ? "root" : rootElementName;

        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var json = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(json))
                return Problem(title: "Corps manquant", detail: "Le corps de la requête doit contenir un JSON valide.", statusCode: 400);

            try
            {
                var xml = JsonConvert.DeserializeXNode(json, rootElementName);
                if (xml == null)
                    return Problem(title: "Conversion JSON → XML impossible", detail: "Le document JSON est vide ou invalide.", statusCode: 400);

                Response.ContentType = "application/xml; charset=utf-8";
                return Ok(xml.ToString());
            }
            catch (JsonException jex)
            {
                return Problem(title: "JSON invalide", detail: jex.Message, statusCode: 400);
            }
        }
        catch (Exception ex)
        {
            return Problem(title: "Échec conversion JSON → XML", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Convertit un flux XML en JSON.
    /// </summary>
    /// <remarks>
    /// <b>Entrée</b> : corps <c>application/xml</c> ou <c>text/xml</c> contenant un document XML bien formé.<br/>
    /// <b>Sortie</b> : <c>application/json</c> compact.<br/>
    /// Paramètre optionnel <c>?noroot=true</c> pour omettre le nœud racine.
    /// </remarks>
    /// <param name="ommitRoot">Si vrai, le nœud racine est omis dans le JSON (défaut : true).</param>
    /// <response code="200">JSON généré avec succès.</response>
    /// <response code="400">XML vide ou invalide.</response>
    /// <response code="500">Erreur interne lors de la conversion.</response>
    [HttpPost("xml2json")]
    [Consumes("application/xml", "text/xml", "application/octet-stream")]
    [Produces("application/json", "application/problem+json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Xml2Json([FromQuery(Name = "noroot")] bool ommitRoot = true)
    {
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var xmlText = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(xmlText))
                return Problem(title: "Corps manquant", detail: "Le corps de la requête doit contenir un document XML.", statusCode: 400);

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                MaxCharactersFromEntities = 10_000,
                MaxCharactersInDocument = 5_000_000,
                XmlResolver = null
            };

            try
            {
                using var stringReader = new StringReader(xmlText);
                using var xmlReader = XmlReader.Create(stringReader, settings);
                var doc = new XmlDocument { XmlResolver = null };
                doc.Load(xmlReader);

                var json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.None, ommitRoot);
                return Content(json, "application/json", Encoding.UTF8);
            }
            catch (XmlException xex)
            {
                return Problem(title: "XML invalide", detail: xex.Message, statusCode: 400);
            }
        }
        catch (Exception ex)
        {
            return Problem(title: "Échec conversion XML → JSON", detail: ex.Message, statusCode: 500);
        }
    }
}
