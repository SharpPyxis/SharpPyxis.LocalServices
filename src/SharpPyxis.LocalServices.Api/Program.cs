using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.OpenApi.Models;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using SharpPyxis.LocalServices.Api.Options;
using SharpPyxis.LocalServices.Api.Services;

var version = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString(3) ?? "0.0.0";

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});

builder.Services.AddProblemDetails();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: "global",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 20
            }));
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(nameof(StorageOptions)));

var swaggerSection = builder.Configuration.GetRequiredSection(nameof(SwaggerOptions));
var swaggerOptions = swaggerSection.Get<SwaggerOptions>()
    ?? throw new ArgumentException($"Invalid section {nameof(SwaggerOptions)} in appsettings.json");

builder.Services.AddSingleton<IMimeMappingService>(_ =>
    new MimeMappingService(new FileExtensionContentTypeProvider()));

builder.Services.AddHttpClient<IOrganizationReferenceService, RechercheEntreprisesOrganizationReferenceService>(client =>
{
    client.BaseAddress = new Uri("https://recherche-entreprises.api.gouv.fr/");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(string.Format(swaggerOptions.DocumentName, version), new OpenApiInfo
    {
        Version = string.Format(swaggerOptions.Version, version),
        Title = string.Format(swaggerOptions.Title, version)
    });

    var entry = Assembly.GetEntryAssembly();
    var assemblyPath = entry?.Location;
    if (!string.IsNullOrWhiteSpace(assemblyPath))
    {
        var assemblyFolder = Path.GetDirectoryName(assemblyPath)!;
        var defaultXml = Path.GetFileNameWithoutExtension(assemblyPath) + ".xml";
        var xmlFile = string.IsNullOrWhiteSpace(swaggerOptions.XmlCommentsFileName)
            ? defaultXml
            : swaggerOptions.XmlCommentsFileName;

        var xmlPath = Path.Combine(assemblyFolder, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    }
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/swagger"))
    {
        var ip = context.Connection.RemoteIpAddress;
        var localOk = IsLocalAddress(ip);
        var hostOk = string.Equals(context.Request.Host.Host, "localhost", StringComparison.OrdinalIgnoreCase);

        if (!localOk || !hostOk)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Swagger UI is only available on loopback with the expected host.");
            return;
        }
    }

    await next();
});

app.UseSwagger(options =>
{
    options.RouteTemplate = swaggerOptions.RouteTemplate;
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        string.Format(swaggerOptions.EndPoint, version),
        string.Format(swaggerOptions.EndPointName, version));
    options.RoutePrefix = swaggerOptions.RoutePrefix;
    options.DocumentTitle = string.Format(swaggerOptions.DocumentTitle, version);
});

app.MapGet("/version", () => Results.Ok(new
{
    name = "SharpPyxis.LocalServices",
    version,
    environment = app.Environment.EnvironmentName
}))
.WithName("Version")
.Produces(StatusCodes.Status200OK);

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }))
    .WithName("Healthz")
    .Produces(StatusCodes.Status200OK);

app.MapControllers();

app.Run();

static bool IsLocalAddress(System.Net.IPAddress? ip)
{
    if (ip == null)
    {
        return false;
    }

    if (System.Net.IPAddress.IsLoopback(ip))
    {
        return true;
    }

    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
    {
        var bytes = ip.GetAddressBytes();
        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168);
    }

    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
    {
        return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.Equals(System.Net.IPAddress.IPv6Loopback);
    }

    return false;
}