using Microsoft.AspNetCore.Components.WebView;

#if WINDOWS
namespace FwLiteMaui;

public partial class MainPage
{
    private partial void BlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
    }

    private partial void BlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        e.WebView.CoreWebView2.PermissionRequested += new SilentPermissionRequestHandler().OnPermissionRequested;
    }
}

#endif
