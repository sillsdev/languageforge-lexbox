using System.Text.Json.Serialization;
using LexCore.Entities;

namespace FwLiteShared;

public class FwLiteConfig
{
    public bool UseDevAssets { get; set; } = false;
    public string AppVersion { get; set; } = "Unknown";
    public FwLitePlatform Os { get; set; } = Environment.OSVersion.Platform switch {
        PlatformID.Win32NT => FwLitePlatform.Windows,
        PlatformID.Unix => FwLitePlatform.Linux,
        PlatformID.MacOSX => FwLitePlatform.Mac,
        _ => FwLitePlatform.Other
    };
    public string FeedbackUrl => $"https://docs.google.com/forms/d/e/1FAIpQLSdUdNufT3sdoBscY7vixguYnvtgpaw-hjX-z54BKi9KlYv4vw/viewform?usp=pp_url&entry.2102942583={AppVersion}&entry.1772086822={Os}";

    private string? _updateUrl;

    public string UpdateUrl
    {
        get
        {
            if (string.IsNullOrEmpty(_updateUrl))
            {
                return $"https://lexbox.org/api/fwlite-release/should-update?appVersion={AppVersion}&edition={Edition}";
            }
            return _updateUrl;
        }
        set => _updateUrl = value;
    }

    private FwLiteEdition? _edition;

    public FwLiteEdition Edition
    {
        get => _edition ??= Os switch
        {
            FwLitePlatform.Android => FwLiteEdition.Android,
            FwLitePlatform.iOS => FwLiteEdition.iOS,
            FwLitePlatform.Linux => FwLiteEdition.Linux,
            FwLitePlatform.Mac => FwLiteEdition.Mac,
            FwLitePlatform.Windows => FwLiteEdition.Windows,
            _ => throw new NotSupportedException($"Unsupported platform: {Os}")
        };
        set => _edition = value;
    }

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
