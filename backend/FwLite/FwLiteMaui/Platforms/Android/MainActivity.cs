using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using FwLiteShared.AppUpdate;
using FwLiteShared.Auth;
using Microsoft.Extensions.DependencyInjection;
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
        //custom style, declared in Android/Resources/values/styles.xml, values-v35 is used based on the android version
        Theme?.ApplyStyle(Resource.Style.OptOutEdgeToEdgeEnforcement, force: false);
        base.OnCreate(savedInstanceState);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);

        // Handle Play Store in-app update flow result
        if (requestCode == AndroidUpdateService.UpdateRequestCode)
        {
            var updateService = MauiApplication.Current.Services.GetService<IPlatformUpdateService>();
            if (updateService is AndroidUpdateService androidUpdateService)
            {
                androidUpdateService.HandleActivityResult(requestCode, resultCode);
            }
        }
    }

    protected override void OnResume()
    {
        base.OnResume();

        // Check for downloaded-but-not-installed updates (flexible update flow)
        var updateService = MauiApplication.Current.Services.GetService<IPlatformUpdateService>();
        if (updateService is AndroidUpdateService androidUpdateService)
        {
            _ = androidUpdateService.CheckForPendingInstallAsync();
        }
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
