using System.Runtime.InteropServices;

namespace FwLiteShared;

public class FwLiteConfig
{
    public bool UseDevAssets { get; set; } = false;
    public string AppVersion { get; set; } = "Unknown";
    public string Os { get; set; } = Environment.OSVersion.Platform switch {
        PlatformID.Win32NT => "Windows",
        PlatformID.Unix => "Linux",
        PlatformID.MacOSX => "Mac",
        _ => "Other"
    };
    public string FeedbackUrl => $"https://lexbox.org/api/feedback/fw-lite?version={AppVersion}&os={Os}";
}
