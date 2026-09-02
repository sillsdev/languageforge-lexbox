using FwLiteShared.Analytics;

namespace FwLiteMaui.Services;

/// <summary>
/// Disables Mixpanel on Firebase Test Lab / Play pre-launch devices.
/// Fresh Play installs by real users do not set <c>firebase.test.lab</c>.
/// </summary>
public sealed class FirebaseTestLabAnalyticsSuppressor : IAnalyticsSuppressor
{
    public bool ShouldSuppress() =>
        MixpanelAnalytics.IsFirebaseTestLabSetting(ReadFirebaseTestLab());

    private static string? ReadFirebaseTestLab()
    {
#if ANDROID
        try
        {
            var resolver = Android.App.Application.Context.ContentResolver;
            var system = Android.Provider.Settings.System.GetString(resolver, "firebase.test.lab");
            if (!string.IsNullOrEmpty(system))
                return system;
            return Android.Provider.Settings.Global.GetString(resolver, "firebase.test.lab");
        }
        catch
        {
            return null;
        }
#else
        return null;
#endif
    }
}
