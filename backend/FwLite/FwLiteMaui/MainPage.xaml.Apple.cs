#if IOS || MACCATALYST
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace FwLiteMaui;

public partial class MainPage
{
    private partial void BlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
    }

    private partial void BlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        // Lets Safari's Develop menu attach Web Inspector to the viewer (iOS 16.4+ / macOS 13.3+).
        // Inspection still requires a trusted Mac and the device's Web Inspector setting, so this is safe to leave on.
        if (OperatingSystem.IsIOSVersionAtLeast(16, 4) || OperatingSystem.IsMacCatalystVersionAtLeast(16, 4))
        {
            e.WebView.Inspectable = true;
        }
    }

    private partial void BlazorWebViewOnUrlLoading(object? sender, UrlLoadingEventArgs e)
    {
        // The app is served from the app:// scheme, so any real web/mail/tel link is external and
        // should open in the system browser or mail client rather than loading inside the WebView
        // (where the user would be stuck with no chrome to navigate back).
        var scheme = e.Url?.Scheme;
        if (scheme is null) return;
        if (scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
            || scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
            || scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase)
            || scheme.Equals("tel", StringComparison.OrdinalIgnoreCase))
        {
            e.UrlLoadingStrategy = UrlLoadingStrategy.CancelLoad;
            _ = Launcher.Default.OpenAsync(e.Url!);
        }
    }
}
#endif
