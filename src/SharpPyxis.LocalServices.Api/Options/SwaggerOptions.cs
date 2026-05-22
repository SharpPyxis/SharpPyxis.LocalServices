namespace SharpPyxis.LocalServices.Api.Options;

/// <summary>
/// Options de configuration de Swagger / OpenAPI.
/// <para>
/// Ces paramètres contrôlent la génération du document JSON (<c>/swagger/{documentName}/swagger.json</c>)
/// et l'interface Swagger UI (<c>/{RoutePrefix}</c>).
/// </para>
/// </summary>
public class SwaggerOptions
{
    /// <summary>
    /// Modèle d'URL du document Swagger JSON.
    /// Exemple par défaut : <c>swagger/{documentName}/swagger.json</c>.
    /// </summary>
    public string RouteTemplate { get; set; } = "swagger/{documentName}/swagger.json";

    /// <summary>
    /// Route publique vers le fichier swagger.json,
    /// typiquement <c>/swagger/{documentName}/swagger.json</c>.
    /// </summary>
    public string EndPoint { get; set; } = "/swagger/{documentName}/swagger.json";

    /// <summary>
    /// Texte affiché dans la liste déroulante d’en-tête de Swagger UI.
    /// Exemple : <c>Local Services API v{0}</c>.
    /// </summary>
    public string EndPointName { get; set; } = "Local Services API v{0}";

    /// <summary>
    /// Chemin de mise à disposition de l’interface Swagger à partir de la racine du site.
    /// Par défaut : <c>swagger</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "swagger";

    /// <summary>
    /// Titre affiché sur la page principale.
    /// Exemple : <c>SharpPyxis.LocalServices v{0}</c>.
    /// </summary>
    public string Title { get; set; } = "SharpPyxis.LocalServices v{0}";

    /// <summary>
    /// Texte affiché en haut à droite du titre (généralement la version).
    /// Exemple : <c>v{0}</c>.
    /// </summary>
    public string Version { get; set; } = "v{0}";

    /// <summary>
    /// Nom du fichier XML de commentaires à inclure.
    /// Laisser vide pour utiliser automatiquement le nom de l’assembly.
    /// </summary>
    public string XmlCommentsFileName { get; set; } = string.Empty;

    /// <summary>
    /// Nom interne du document Swagger (utilisé dans les routes et non affiché).
    /// Exemple : <c>v{0}</c>.
    /// </summary>
    public string DocumentName { get; set; } = "v{0}";

    /// <summary>
    /// Libellé de l’onglet du navigateur (titre HTML).
    /// Exemple : <c>SharpPyxis.LocalServices</c>.
    /// </summary>
    public string DocumentTitle { get; set; } = "SharpPyxis.LocalServices";
}
