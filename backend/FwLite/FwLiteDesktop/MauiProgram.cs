using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace FwLiteDesktop;

public static class MauiProgram
{
    private record AppHolder(MauiApp? App)
    {
        public MauiApp? App { get; set; } = App;
    }

    public static MauiApp CreateMauiApp()
    {
        // AppHolder holder = new AppHolder(null);
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        Directory.CreateDirectory(FileSystem.AppDataDirectory);
        builder.Services.AddFwLiteDesktopServices(builder.Configuration, builder.Logging);

        var app = builder.Build();
        var logger = app.Services.GetRequiredService<ILogger<MauiApp>>();
        logger.LogInformation("App started");
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                logger.LogError(exception, "Unhandled exception");
            }
            else
            {
                logger.LogError("Unhandled exception");
            }
        };
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            logger.LogError(e.Exception, "Unobserved task exception");
        };
        return app;
    }
}
