using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteDesktop;

public partial class MainPage : ContentPage
{
    public MainPage(IOptionsMonitor<LocalWebAppConfig> options, ILogger<MainPage> logger)
    {
        InitializeComponent();
        options.OnChange(o =>
        {
            var url = o.Url;
            webView.Dispatcher.Dispatch(() => webView.Source = url);
            logger.LogInformation("Url updated: {Url}", url);
        });
        var url = options.CurrentValue.Url;
        webView.Source = url;
        logger.LogInformation("Main page initialized, url: {Url}", url);
    }
}

