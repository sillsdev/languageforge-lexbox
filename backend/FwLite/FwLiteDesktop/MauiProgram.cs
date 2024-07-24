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
    private record AppHolder(MauiApp? App)
    {
        public MauiApp? App { get; set; } = App;
    }

    public static MauiApp CreateMauiApp()
    {
        AppHolder holder = new AppHolder(null);
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        builder.ConfigureLifecycleEvents(events => events.AddWindows(windowsEvents =>
        {
            windowsEvents.OnClosed((window, args) =>
            {
                holder.App?.Services.GetRequiredService<ServerManager>().Stop();
            });
        }));

        Directory.CreateDirectory(FileSystem.AppDataDirectory);
        builder.Services.AddFwLiteDesktopServices(builder.Configuration, builder.Logging);

        holder.App = builder.Build();
        var logger = holder.App.Services.GetRequiredService<ILogger<MauiApp>>();
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
        return holder.App;
    }
}
