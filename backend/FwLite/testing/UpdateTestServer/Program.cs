using System.Text.Json;
using LexCore.Entities;

// Fake lexbox FwLite update API for local update testing. Mirrors the two things a Windows
// FwLite client touches during an update: GET /api/fwlite-release/should-update, and the
// .msixbundle download that release.url points at (served here with HEAD + Range so the
// in-app loopback download proxy can probe and stream it).
//
// Config (via command line, e.g. --port 5199 --serve v2 --bundle-dir C:\...\artifacts\harness):
//   port       loopback port to listen on (default 5199)
//   serve      which build to offer as "the update": v1 | v2 | none (default none)
//   bundle-dir folder holding builds.json + the *.msixbundle files (required)

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss ");

var port = builder.Configuration.GetValue("port", 5199);
var serve = builder.Configuration.GetValue("serve", "none")!.Trim().ToLowerInvariant();
var bundleDir = builder.Configuration.GetValue<string?>("bundle-dir")
    ?? throw new InvalidOperationException("--bundle-dir is required");

builder.WebHost.UseUrls($"http://localhost:{port}");

var app = builder.Build();
var log = app.Logger;

// Resolve which release (if any) we're serving from builds.json written by the build step.
var served = ResolveServed(bundleDir, serve, log);
if (served is null)
    log.LogInformation("Serving NO update (serve='{Serve}'): should-update will report up-to-date", serve);
else
    log.LogInformation("Serving update {Version} -> {File}", served.Value.Version, served.Value.File);

app.Use(async (ctx, next) =>
{
    log.LogInformation("{Method} {Path}{Query}", ctx.Request.Method, ctx.Request.Path, ctx.Request.QueryString);
    await next();
});

// The client's UpdateChecker GETs this and trusts whatever release we return (it does no version
// comparison of its own). The override UpdateUrl carries no query string, but the client still sends
// its current version in the User-Agent ("Fieldworks-Lite-Client/{AppVersion}"), so — like the real
// FwLiteReleaseService — we only offer the update when the served version sorts strictly newer.
// That way "serve v1" with v1 installed reports up-to-date; "serve v2" prompts.
app.MapGet("/api/fwlite-release/should-update", (HttpContext ctx) =>
{
    if (served is not { } s) return Results.Ok(new ShouldUpdateResponse(null));

    var clientVersion = ParseClientVersion(ctx.Request.Headers.UserAgent.ToString());
    if (clientVersion is not null && string.Compare(s.Version, clientVersion, StringComparison.Ordinal) <= 0)
    {
        log.LogInformation("Client is on {Client}; served {Served} is not newer -> reporting up-to-date",
            clientVersion, s.Version);
        return Results.Ok(new ShouldUpdateResponse(null));
    }

    var url = $"http://localhost:{port}/download/{s.File}";
    return Results.Ok(new ShouldUpdateResponse(new FwLiteRelease(s.Version, url)));
});

// The bundle the loopback proxy downloads. enableRangeProcessing gives us HEAD + 206/Range,
// which the proxy relies on (ResolveFinalUrl does a HEAD; Windows differential updates use Range).
app.MapMethods("/download/{file}", ["GET", "HEAD"], (string file) =>
{
    // Guard against path traversal: only serve a bare file name from the bundle dir.
    if (file.Contains('/') || file.Contains('\\') || file.Contains("..")) return Results.NotFound();
    var path = Path.Combine(bundleDir, file);
    return File.Exists(path)
        ? Results.File(path, "application/octet-stream", enableRangeProcessing: true)
        : Results.NotFound();
});

app.Run();

// Pull the version out of "Fieldworks-Lite-Client/{AppVersion}" (the header UpdateChecker sends).
static string? ParseClientVersion(string userAgent)
{
    const string prefix = "Fieldworks-Lite-Client/";
    var idx = userAgent.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
    if (idx < 0) return null;
    var version = userAgent[(idx + prefix.Length)..].Split(' ')[0].Trim();
    return string.IsNullOrEmpty(version) ? null : version;
}

static (string Version, string File)? ResolveServed(string bundleDir, string serve, ILogger log)
{
    if (serve is "none" or "") return null;

    var buildsJson = Path.Combine(bundleDir, "builds.json");
    if (!File.Exists(buildsJson))
    {
        log.LogWarning("builds.json not found at {Path}; nothing to serve", buildsJson);
        return null;
    }

    using var doc = JsonDocument.Parse(File.ReadAllText(buildsJson));
    if (!doc.RootElement.TryGetProperty(serve, out var entry))
    {
        log.LogWarning("builds.json has no entry '{Serve}'", serve);
        return null;
    }

    var version = entry.GetProperty("version").GetString();
    var file = entry.GetProperty("file").GetString();
    if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(file)) return null;
    return (version, file);
}
