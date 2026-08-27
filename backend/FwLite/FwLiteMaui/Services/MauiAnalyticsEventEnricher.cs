using FwLiteShared.Analytics;

namespace FwLiteMaui.Services;

/// <summary>
/// Adds Mixpanel <c>$android_os_version</c> on Android. Other MAUI platforms contribute nothing.
/// </summary>
public class MauiAnalyticsEventEnricher : IAnalyticsEventEnricher
{
    public void Enrich(Dictionary<string, object?> properties)
    {
        if (DeviceInfo.Current.Platform != DevicePlatform.Android)
            return;
        var version = DeviceInfo.Current.VersionString;
        if (string.IsNullOrEmpty(version))
            return;
        properties["$android_os_version"] = version;
    }
}
