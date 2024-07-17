using FwLiteDesktop.ServerBridge;
using LcmCrdt;
using LocalWebApp;
using LocalWebApp.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using NReco.Logging.File;

namespace FwLiteDesktop;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        Directory.CreateDirectory(FileSystem.AppDataDirectory);
        builder.Services.AddSingleton<MainPage>();

        var serverManager = new ServerManager(webAppBuilder =>
        {
            webAppBuilder.Logging.AddFile(Path.Combine(FileSystem.AppDataDirectory, "web-app.log"));
            webAppBuilder.Services.Configure<LcmCrdtConfig>(config =>
            {
                config.ProjectPath = FileSystem.AppDataDirectory;
            });
            webAppBuilder.Services.Configure<AuthConfig>(config =>
                config.CacheFileName = Path.Combine(FileSystem.AppDataDirectory, "msal.cache"));
        });
        builder.Services.AddSingleton(serverManager);
        builder.Configuration.Add<ServerConfigSource>(source => source.ServerManager = serverManager);
        builder.Services.AddOptions<LocalWebAppConfig>().BindConfiguration("LocalWebApp");
        builder.Logging.AddFile(Path.Combine(FileSystem.AppDataDirectory, "app.log"));
#if DEBUG
        builder.Logging.AddDebug();
#endif
        var app = builder.Build();
        app.Services.GetRequiredService<ServerManager>()
            .Start(app.Services.GetRequiredService<ILogger<ServerManager>>());
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
