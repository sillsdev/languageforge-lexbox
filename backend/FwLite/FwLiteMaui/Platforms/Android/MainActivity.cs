using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using FwLiteMaui.Platforms.Android;
using FwLiteShared.Auth;
using Microsoft.Identity.Client;

namespace FwLiteMaui;

[Activity(Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Android 15+ (SDK 35) enforces edge-to-edge: the previous opt-out attribute
        // (windowOptOutEdgeToEdgeEnforcement) is ignored when targeting SDK 36+, so the
        // WebView would otherwise draw under the status / gesture-nav bars. Rather than
        // pushing inset CSS vars into the WebView and reworking the frontend, we pad the
        // activity's content view inward so the WebView sits in the safe area like it
        // did pre-SDK-35, with the window background showing through as a themed band
        // behind the system bars.
        AndroidSafeAreaPadding.Install(this);
    }

    public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        // ConfigChanges.UiMode means we don't get recreated on dark/light toggle, but
        // the colored window background DOES re-resolve from the day/night resource.
        // The status-bar icon appearance is sticky though, so re-pick it here.
        AndroidSafeAreaPadding.ApplyStatusBarIconAppearance(this);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
    }
}


[Activity(Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
    DataHost = "auth",
    DataScheme = "msal" + AuthConfig.DefaultClientId)]
public class MsalActivity : BrowserTabActivity
{
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
    }
}
