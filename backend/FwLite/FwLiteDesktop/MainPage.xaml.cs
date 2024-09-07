using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteDesktop;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage> _logger;
    private readonly IHostEnvironment _environment;

    public MainPage(IOptionsMonitor<LocalWebAppConfig> options, ILogger<MainPage> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        InitializeComponent();
        options.OnChange(o =>
        {
            var url = o.Url;
            webView.Dispatcher.Dispatch(() => webView.Source = url);
            logger.LogInformation("Url updated: {Url}", url);
        });
        webView.IsVisible = false;
        var url = options.CurrentValue.Url;
        webView.Source = url;
        logger.LogInformation("Main page initialized, url: {Url}", url);
        webView.Navigating += (sender, args) =>
        {
            logger.LogInformation("Web view navigating to {Url}", args.Url);
        };
        int navigatedCount = 0;
        int maxNavigatedCount = 10;
        webView.Navigated += (sender, args) =>
        {
            logger.LogInformation("Web view navigated to {Url}, result: {Result}", args.Url, args.Result);
            if (args.Url.StartsWith("https://appdir") && options.CurrentValue.Url is not null)
            {
                if (++navigatedCount <= maxNavigatedCount)
                {
                    logger.LogInformation("Navigating to {Url}", options.CurrentValue.Url);
                    webView.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(100 * navigatedCount),
                        () => webView.Source = options.CurrentValue.Url);
                }
                else
                {
                    logger.LogWarning("Too many navigations, stopping");
                }
            }
            else if (!args.Url.StartsWith("https://appdir"))
            {
                NavigationSuccess();
            }
        };
    }

    private void NavigationSuccess()
    {
        webView.IsVisible = true;
        //not currently working
        // if (_environment.IsDevelopment())
        {
            //lang=js
            webView.Eval("""
                         localStorage.setItem('devMode', 'true');
                         if (enableDevMode) enableDevMode();
                         """);
        }
    }
}
