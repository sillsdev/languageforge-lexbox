#if WINDOWS
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using Microsoft.Web.WebView2.Core;
using FocusState = Microsoft.UI.Xaml.FocusState;
using WebView2Control = Microsoft.UI.Xaml.Controls.WebView2;

namespace FwLiteMaui;

public partial class MainPage
{
    //what a new window shows until WebView2 has painted; index.html continues in the same colours
    private static Color LoadingBackground =>
        Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#09090b") : Colors.White;

    // WinUI 1.8 aborts the app (0xc0000409) if the WebView2 got focus while initializing and the window is minimized
    // before that ends: it replays the focus via CoreWebView2Controller.MoveFocus, which throws E_INVALIDARG inside a
    // noexcept handler (fixed in microsoft/microsoft-ui-xaml#11978, not in 1.8). So keep focus off it until it's ready.
    private partial void BlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
        BackgroundColor = LoadingBackground;
        if (blazorWebView.Handler?.PlatformView is WebView2Control webView) webView.IsTabStop = false;
    }

    private static void FocusWhenNotMinimized(WebView2Control webView)
    {
        webView.IsTabStop = true;
        if (webView.XamlRoot?.ContentIslandEnvironment is not { } environment
            || AppWindow.GetFromWindowId(environment.AppWindowId) is not { } appWindow) return;
        if (!IsMinimized(appWindow))
        {
            webView.Focus(FocusState.Programmatic);
            return;
        }

        void OnChanged(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (IsMinimized(sender)) return;
            sender.Changed -= OnChanged;
            webView.Focus(FocusState.Programmatic);
        }
        appWindow.Changed += OnChanged;
    }

    private static bool IsMinimized(AppWindow appWindow) =>
        appWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized };

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
        e.WebView.DefaultBackgroundColor = LoadingBackground.ToWindowsColor();
        var webView = e.WebView;
        // queued so it runs after WinUI has finished initializing the control
        webView.DispatcherQueue.TryEnqueue(() => FocusWhenNotMinimized(webView));
    }
}

#endif
