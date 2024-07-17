using FwLiteDesktop.ServerBridge;
using LocalWebApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

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

        builder.Services.AddSingleton<MainPage>();

        var serverManager = new ServerManager();
        builder.Services.AddSingleton(serverManager);
        builder.Configuration.Add<ServerConfigSource>(source => source.ServerManager = serverManager);
        builder.Services.AddOptions<LocalWebAppConfig>().BindConfiguration("LocalWebApp");

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        app.Services.GetRequiredService<ServerManager>().Start();
        return app;
    }
}
