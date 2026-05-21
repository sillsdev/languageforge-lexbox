using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using AndroidX.Core.View;
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
        ApplyBrandedSystemBars();
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        // MAUI re-decides system-bar icon appearance on config changes, so
        // re-pin it after the base call.
        ApplyBrandedSystemBars();
    }

    // Paints the system-bar gutter left around the BlazorWebView (by
    // MainPage's SafeAreaEdges="Container") with the brand color and pins
    // the icons to light so they stay readable regardless of system theme
    // or in-app theme overrides.
    private void ApplyBrandedSystemBars()
    {
        if (Window is null) return;
        Window.SetBackgroundDrawableResource(Resource.Color.colorPrimaryDark);
        var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
        if (controller is null) return;
        controller.AppearanceLightStatusBars = false;
        controller.AppearanceLightNavigationBars = false;
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
