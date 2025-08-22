#if WINDOWS
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Web.WebView2.Core;

namespace FwLiteMaui;

public partial class MainPage
{
    private partial void BlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
    }

    private partial void BlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        //we only need microphone for now, but we'll just set them all so we don't have to worry about it later
        Span<CoreWebView2PermissionKind> permissions =
        [
            CoreWebView2PermissionKind.Microphone,
            CoreWebView2PermissionKind.Autoplay,
            CoreWebView2PermissionKind.Camera,
            CoreWebView2PermissionKind.Geolocation,
            CoreWebView2PermissionKind.Notifications,
            CoreWebView2PermissionKind.ClipboardRead,
            CoreWebView2PermissionKind.FileReadWrite,
            CoreWebView2PermissionKind.LocalFonts,
            CoreWebView2PermissionKind.MultipleAutomaticDownloads,
            CoreWebView2PermissionKind.OtherSensors,
            CoreWebView2PermissionKind.WindowManagement,
        ];
#pragma warning disable VSTHRD110, CS4014
        foreach (var permission in permissions)
        {
            e.WebView.CoreWebView2.Profile.SetPermissionStateAsync(permission,
                "https://0.0.0.1",
                CoreWebView2PermissionState.Allow);
            e.WebView.CoreWebView2.Profile.SetPermissionStateAsync(permission,
                "https://0.0.0.0",
                CoreWebView2PermissionState.Allow);
        }
#pragma warning restore VSTHRD110, CS4014
        e.WebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
    }
}

#endif
