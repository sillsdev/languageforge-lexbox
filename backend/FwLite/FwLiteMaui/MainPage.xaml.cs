using FwLiteShared.Services;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FwLiteMaui;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage> _logger;

    public MainPage(IPreferencesService preferences, ILogger<MainPage> logger)
    {
        _logger = logger;
        InitializeComponent();
        var lastUrlFromPrefs = preferences.Get(nameof(PreferenceKey.AppLastUrl));
        blazorWebView.BlazorWebViewInitializing += BlazorWebViewInitializing;
        blazorWebView.BlazorWebViewInitialized += BlazorWebViewInitialized;
        blazorWebView.BlazorWebViewInitialized += (s, e) =>
        {
            // Decide initial StartPath here (after Android intent is processed) to avoid cold-start race
            var initial = App.OverrideStartupUrl ?? lastUrlFromPrefs ?? "/";
            //only change it if it's still the default, might have been changed already, for example when opening a new window
            if (blazorWebView.StartPath == "/")
                blazorWebView.StartPath = initial;
            App.OverrideStartupUrl = null;
        };
        blazorWebView.UrlLoading += BlazorWebViewOnUrlLoading;
    }

    internal string StartPath
    {
        get => blazorWebView.StartPath;
        set => blazorWebView.StartPath = value;
    }

    internal void LoadAppUrl(string url)
    {
        blazorWebView.StartPath = url;
        _ = blazorWebView.TryDispatchAsync(services =>
        {
            var jsRuntime = services.GetService<IJSRuntime>();
            if (jsRuntime != null)
                _ = NavigateAsync(jsRuntime, url);
        });
    }

    private async Task NavigateAsync(IJSRuntime jsRuntime, string url)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("lexbox.SvelteNavigate", url, new { replace = true });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to navigate to {Url} via SvelteNavigate", url);
        }
    }

#if ANDROID || WINDOWS
    private partial void BlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e);
    private partial void BlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e);
#else
    private void BlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
    }

    private void BlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
    }
#endif

#if ANDROID
    private partial void BlazorWebViewOnUrlLoading(object? sender, UrlLoadingEventArgs e);
#else

    private void BlazorWebViewOnUrlLoading(object? sender, UrlLoadingEventArgs e)
    {
    }

#endif
}
