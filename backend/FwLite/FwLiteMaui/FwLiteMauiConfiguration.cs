using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FwLiteMaui;

/// <summary>
/// Composes the MAUI app configuration. MAUI loads nothing by default (https://github.com/dotnet/maui/issues/4408),
/// so precedence is: <see cref="FwLiteMauiKernel.DefaultConfiguration"/> &lt; <c>fw-lite-settings.json</c> in the
/// data dir &lt; environment variables.
/// The settings file is what support asks users to create (or the troubleshoot dialog writes) instead of env vars.
/// </summary>
public static class FwLiteMauiConfiguration
{
    public const string SettingsFileName = "fw-lite-settings.json";
    private const string MsalLogCategory = "FwLiteShared.Auth.LoggerAdapter";
    private const string DefaultLogLevelKey = "Logging:LogLevel:Default";

    public static void AddFwLiteMauiConfiguration(this ConfigurationManager configuration)
    {
        //the settings file lives in the data dir, so only an env var (or the platform default) can say where that is
        var envOnly = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var baseDataDir = (envOnly.GetSection("FwLiteMaui").Get<FwLiteMauiConfig>() ?? new()).BaseDataDir;
        configuration.AddFwLiteMauiConfiguration(Path.Combine(baseDataDir, SettingsFileName),
            //a file watcher is only useful where users edit the file by hand; the in-app toggle calls Reload() itself
            reloadOnChange: OperatingSystem.IsWindows() || OperatingSystem.IsMacCatalyst());
    }

    internal static void AddFwLiteMauiConfiguration(this IConfigurationBuilder builder,
        string settingsFilePath,
        bool reloadOnChange)
    {
        builder.AddInMemoryCollection(FwLiteMauiKernel.DefaultConfiguration);
        //any section can be set here, same as via environment variables; the file is in the user's own data dir
        //so it is no more a trust boundary than they are
        builder.AddJsonFile(source =>
        {
            source.Path = settingsFilePath;
            source.Optional = true;
            source.ReloadOnChange = reloadOnChange;
            //a hand-edited file with a syntax error must not take the app down (the default rethrows, at startup
            //before any logger exists). The file is skipped and reported by LogSettingsFile instead.
            source.OnLoadException = context =>
            {
                context.Ignore = true;
                LastLoadError = context.Exception;
            };
            source.ResolveFileProvider();
        });
        builder.AddEnvironmentVariables();
    }

    internal static volatile Exception? LastLoadError;

    /// <summary>So a log shared with support shows which sections a settings file is overriding, or why it was skipped.</summary>
    public static void LogSettingsFile(string settingsFilePath, ILogger logger)
    {
        if (!File.Exists(settingsFilePath)) return;
        if (LastLoadError is { } error)
        {
            logger.LogError(error, "Ignoring {SettingsFile} because it could not be read", settingsFilePath);
            return;
        }

        var sections = string.Join(", ", ReadSettingsFile(settingsFilePath).Select(p => p.Key));
        logger.LogInformation("Loaded {SettingsFile} with sections: {Sections}", settingsFilePath, sections);
    }

    //no Default entry means the LoggerFactory default, Information
    public static bool IsVerboseLogging(IConfiguration configuration) =>
        Enum.TryParse<LogLevel>(configuration[DefaultLogLevelKey], ignoreCase: true, out var level) &&
        level <= LogLevel.Debug;

    /// <summary>
    /// Turns verbose logging on or off by editing only the log levels it owns in the settings file; anything else
    /// in the file is kept (comments are not).
    /// </summary>
    public static void SetVerboseLogging(string settingsFilePath, bool enabled)
    {
        var root = ReadSettingsFile(settingsFilePath);
        var logging = root["Logging"] as JsonObject ?? (JsonObject)(root["Logging"] = new JsonObject());
        var logLevel = logging["LogLevel"] as JsonObject ?? (JsonObject)(logging["LogLevel"] = new JsonObject());
        if (enabled)
        {
            logLevel["Default"] = "Debug";
            logLevel[MsalLogCategory] = "Debug";
        }
        else
        {
            logLevel.Remove("Default");
            logLevel.Remove(MsalLogCategory);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath)!);
        File.WriteAllText(settingsFilePath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Replaces the whole file with user-supplied text; blank deletes it. Rejects anything that isn't a JSON object.</summary>
    public static void WriteSettingsFile(string settingsFilePath, string contents)
    {
        if (string.IsNullOrWhiteSpace(contents))
        {
            File.Delete(settingsFilePath);
            return;
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(contents, documentOptions: ParseOptions);
        }
        catch (JsonException e)
        {
            throw new ArgumentException($"Settings are not valid JSON: {e.Message}", nameof(contents));
        }

        if (node is not JsonObject) throw new ArgumentException("Settings must be a JSON object", nameof(contents));
        Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath)!);
        File.WriteAllText(settingsFilePath, contents);
    }

    //the configuration loader tolerates comments and trailing commas, so do the same here
    private static readonly JsonDocumentOptions ParseOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static JsonObject ReadSettingsFile(string path)
    {
        if (!File.Exists(path)) return new JsonObject();
        var node = JsonNode.Parse(File.ReadAllText(path),
            new JsonNodeOptions { PropertyNameCaseInsensitive = true },
            ParseOptions);
        return node as JsonObject ?? new JsonObject();
    }
}
