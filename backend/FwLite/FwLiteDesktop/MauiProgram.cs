using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace FwLiteDesktop;

public static class MauiProgram
{
    private record AppHolder(MauiApp? App)
    {
        public MauiApp? App { get; set; } = App;

        public void Shutdown()
        {
            var thread = new Thread(() =>
            {
                if (App == null) return;

                var logger = App.Services.GetRequiredService<ILogger<MauiApp>>();
                try
                {
                    logger.LogInformation("Disposing app");
                    App.DisposeAsync().GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to dispose app");
                    throw;
                }
            })
            {
                IsBackground = false
            };
            thread.Start();
        }
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
        builder.ConfigureEssentials(essentialsBuilder =>
        {
            essentialsBuilder.UseVersionTracking();
        });
        builder.Services.AddFwLiteDesktopServices(builder.Configuration, builder.Logging);

        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android
                .OnDestroy((activity) => holder.Shutdown()));
#endif
#if IOS || MACCATALYST
            events.AddiOS(ios => ios
                .WillTerminate((app) => holder.Shutdown()));
#endif
#if WINDOWS
            events.AddWindows(windows => windows
                .OnClosed((window, args) => holder.Shutdown()));
#endif
        });

        var app = builder.Build();
        holder.App = app;
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
