using Microsoft.Extensions.Options;

namespace FwLiteDesktop;

public partial class MainPage : ContentPage
{
    public MainPage(IOptionsMonitor<LocalWebAppConfig> options)
    {
        InitializeComponent();
        options.OnChange(o =>
        {
            webView.Dispatcher.Dispatch(() => webView.Source = o.Url);
        });
        webView.Source = options.CurrentValue.Url;
    }
}

