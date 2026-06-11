using FwLiteShared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteMaui;

public partial class MainPage : ContentPage
{
    public MainPage(IPreferencesService preferences)
    {
        InitializeComponent();
        blazorWebView.BlazorWebViewInitializing += BlazorWebViewInitializing;
        blazorWebView.BlazorWebViewInitialized += BlazorWebViewInitialized;
        blazorWebView.UrlLoading += BlazorWebViewOnUrlLoading;

        var lastUrl = preferences.Get(nameof(PreferenceKey.AppLastUrl));
        #if ANDROID
        var intent = Platform.CurrentActivity?.Intent;
        if (intent is not null && intent.Action == "ACTION_XE_APP_ACTION")
        {
            var actionId = intent.GetStringExtra("EXTRA_XE_APP_ACTION_ID");
            if (actionId is "home") lastUrl = "/home";
        }
        #endif
        if (lastUrl?.StartsWith('/') == true)
        {
            StartPath = lastUrl;
        }
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
            var navigationManager = services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo(url);
            //the svelte-routing library doesn't notice the url change via NavigateTo, so just reload the page
            navigationManager.Refresh();
        });
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
