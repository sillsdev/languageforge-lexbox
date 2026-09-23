using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using AndroidX.Core.View;
using FwLiteShared.Auth;
using Microsoft.Extensions.DependencyInjection;
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
    private AndroidInAppUpdateService? _inAppUpdateService;
    private bool _hasResumed;

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
        StartInAppUpdateCheck();
    }

    protected override void OnResume()
    {
        base.OnResume();
        Platform.OnResume(this);
        //Skip the first resume: it fires right after OnCreate's CheckForUpdate, which already handles a
        //previously-downloaded update, so checking again here would risk a second dialog. Later resumes
        //(returning from background) still catch a download that finished while we were away.
        if (_hasResumed)
        {
            _inAppUpdateService?.CheckForDownloadedUpdateOnResume();
        }
        _hasResumed = true;
    }

    private void StartInAppUpdateCheck()
    {
        //Never let a Play update check crash startup - it's a best-effort nicety.
        try
        {
            //Container-owned singleton (disposed at shutdown); don't dispose it from the activity.
            _inAppUpdateService = IPlatformApplication.Current?.Services.GetService<AndroidInAppUpdateService>();
            _inAppUpdateService?.CheckForUpdate(this);
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start in-app update check: {e}");
        }
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
        if (requestCode == AndroidInAppUpdateService.UpdateRequestCode)
        {
            //Play's update flow result. Nothing to do: if the user declined, Play offers again later;
            //if they accepted, the download proceeds in the background. Don't forward to MSAL.
            return;
        }
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
