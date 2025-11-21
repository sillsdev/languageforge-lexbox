#if ANDROID

using Android.Content;
using AndroidX.Activity;
using FwLiteMaui.Platforms.Android;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Maui.Platform;

namespace FwLiteMaui;

public partial class MainPage
{
    // To manage Android permissions, update AndroidManifest.xml to include the permissions and
    // features required by your app. You may have to perform additional configuration to enable
    // use of those APIs from the WebView, as is done below. A custom WebChromeClient is needed
    // to define what happens when the WebView requests a set of permissions. See
    // PermissionManagingBlazorWebChromeClient.cs to explore the approach taken in this example.

    private partial void BlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
    }

    private partial void BlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        if (e.WebView.Context?.GetActivity() is not ComponentActivity activity)
        {
            throw new InvalidOperationException(
                $"The permission-managing WebChromeClient requires that the current activity be a '{nameof(ComponentActivity)}'.");
        }

        e.WebView.Settings.JavaScriptEnabled = true;
        e.WebView.Settings.AllowFileAccess = true;
        e.WebView.Settings.MediaPlaybackRequiresUserGesture = false;
        e.WebView.Settings.SetGeolocationEnabled(true);
        var baseClient = OperatingSystem.IsAndroidVersionAtLeast(26)
            ? (e.WebView.WebChromeClient ?? new Android.Webkit.WebChromeClient())
            : new Android.Webkit.WebChromeClient();
        e.WebView.SetWebChromeClient(new PermissionManagingBlazorWebChromeClient(baseClient, activity));
    }

    private partial void BlazorWebViewOnUrlLoading(object? sender, UrlLoadingEventArgs e)
    {
        if ("mailto".Equals(e.Url?.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            var intent = new Intent(action: Intent.ActionSendto, Android.Net.Uri.Parse(e.Url.ToString()));
            intent.AddFlags(ActivityFlags.NewTask);
            Platform.AppContext.StartActivity(intent);
            e.UrlLoadingStrategy = UrlLoadingStrategy.CancelLoad;
        }
    }
}

#endif
