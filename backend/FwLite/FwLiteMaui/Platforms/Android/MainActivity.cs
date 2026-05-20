using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
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
        // Android 15+ (SDK 35) enforces edge-to-edge by default. Rather than opting out
        // (which has historically been fragile - the attribute is deprecated on Android 16+
        // and CSS env(safe-area-inset-*) inside the BlazorWebView has been observed to
        // return 0 even with viewport-fit=cover) we embrace edge-to-edge and propagate
        // the real system-bar insets to the WebView as CSS custom properties via
        // AndroidEdgeToEdgeInsets (installed when the BlazorWebView is initialized).
        base.OnCreate(savedInstanceState);
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
