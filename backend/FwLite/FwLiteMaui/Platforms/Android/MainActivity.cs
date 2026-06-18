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
    ResizeableActivity = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter([Platform.Intent.ActionAppAction],
    Categories = [Intent.CategoryDefault])]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (Intent?.Action == Platform.Intent.ActionAppAction)
        {
            //name comes from internal maui code: https://github.com/dotnet/maui/blob/271d2505eb436600bb84002c8941670abb0ae23b/src/Essentials/src/AppActions/AppActions.android.cs#L83
            var actionId = Intent.GetStringExtra("EXTRA_XE_APP_ACTION_ID");
            if (Shortcuts.TryGetUrl(actionId, out var url))
            {
                App.OverrideStartupUrl = url;
            }
        }

        ApplyBrandedSystemBars();
    }

    protected override void OnResume()
    {
        base.OnResume();
        Platform.OnResume(this);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Platform.OnNewIntent(intent);
    }
    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        ApplyBrandedSystemBars();
    }

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
