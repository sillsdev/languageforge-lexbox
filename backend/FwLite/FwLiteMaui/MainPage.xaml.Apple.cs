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
}
#endif
