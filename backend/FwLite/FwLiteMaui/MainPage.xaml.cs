using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteMaui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        blazorWebView.BlazorWebViewInitializing += BlazorWebViewInitializing;
        blazorWebView.BlazorWebViewInitialized += BlazorWebViewInitialized;
        blazorWebView.UrlLoading += BlazorWebViewOnUrlLoading;
    }



    internal string StartPath
    {
        get => blazorWebView.StartPath;
        set => blazorWebView.StartPath = value;
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
