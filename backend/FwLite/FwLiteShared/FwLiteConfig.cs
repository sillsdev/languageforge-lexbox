using System.Runtime.InteropServices;

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
}

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
