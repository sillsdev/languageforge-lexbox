using System.Text.Json.Serialization;
using LexCore.Entities;

namespace FwLiteShared;

public class FwLiteConfig
{
    public bool UseDevAssets { get; set; } = false;
    //Vite dev-server port the browser pulls viewer assets from when UseDevAssets is true; override via env FwLite__DevAssetsPort so parallel worktrees can each run their own dev server.
    public int DevAssetsPort { get; set; } = 5173;
    public string AppVersion { get; set; } = "Unknown";
    public FwLitePlatform Os { get; set; } = Environment.OSVersion.Platform switch {
        PlatformID.Win32NT => FwLitePlatform.Windows,
        PlatformID.Unix => FwLitePlatform.Linux,
        PlatformID.MacOSX => FwLitePlatform.Mac,
        _ => FwLitePlatform.Other
    };
    public string FeedbackUrl => $"https://docs.google.com/forms/d/e/1FAIpQLSetM5LIjrwZ8ibmxz9gHwYDqTqpEzsr3ILzrf_dW1rOoRf9rw/viewform?usp=pp_url&entry.1159558932={AppVersion}&entry.866582270={Os}";

    /// <summary>
    /// Replaces the release-feed URL, e.g. env FwLite__OverrideUpdateUrl pointing at the local update test server.
    /// Leave unset to ask lexbox.org; use <see cref="ResolveUpdateUrl"/> to get the URL to call.
    /// </summary>
    public string? OverrideUpdateUrl { get; set; }

    //a method, not a property: configuration binding reads every settable property and writes the value back,
    //which froze a computed URL with the default AppVersion ("Unknown") before the host set the real one
    public string ResolveUpdateUrl()
    {
        return string.IsNullOrEmpty(OverrideUpdateUrl)
            ? $"https://lexbox.org/api/fwlite-release/should-update?appVersion={AppVersion}&edition={Edition}"
            : OverrideUpdateUrl;
    }

    //derived on every read, without a setter or cache, for the same reason: binding runs before the host sets Os
    public FwLiteEdition Edition => Os switch
    {
        FwLitePlatform.Android => FwLiteEdition.Android,
        FwLitePlatform.iOS => FwLiteEdition.iOS,
        FwLitePlatform.Linux => FwLiteEdition.Linux,
        FwLitePlatform.Mac => FwLiteEdition.Mac,
        FwLitePlatform.Windows => FwLiteEdition.Windows,
        _ => throw new NotSupportedException($"Unsupported platform: {Os}")
    };

    //can be configured with env FwLite__UpdateCheckCondition
    public UpdateCheckCondition UpdateCheckCondition { get; set; } = UpdateCheckCondition.OnInterval;
    public TimeSpan UpdateCheckInterval { get; set; } = TimeSpan.FromHours(8);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FwLitePlatform
{
    Windows,
    Linux,
    Mac,
    Other,
    Android,
    // ReSharper disable once InconsistentNaming
    iOS,
    Web
}

public enum UpdateCheckCondition
{
    Always,
    Never,
    OnInterval
}
