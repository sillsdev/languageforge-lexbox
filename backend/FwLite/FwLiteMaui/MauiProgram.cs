using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace FwLiteMaui;

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
                var adapter = App.Services.GetRequiredService<FwLiteMauiKernel.HostedServiceAdapter>();
                try
                {
                    logger.LogInformation("Disposing hosted services");
                    //I tried to dispose of the app, but that caused other issues on shutdown, so we're just going to dispose of the hosted services
#pragma warning disable VSTHRD002
                    adapter.DisposeAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to dispose hosted services");
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
        builder.Services.AddFwLiteMauiServices(builder.Configuration, builder.Logging);

        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android
                .OnDestroy((activity) =>
                {
                    // This doesn't work, because OnDestroy gets called e.g. when logging in via OAuth. (and maybe whenever the app is backgrounded?)
                    // It seems like there is no appropriate hook. Maybe we're just supposed to let the OS kill the app?
                    //holder.Shutdown();
                }));
#endif
#if IOS || MACCATALYST
            events.AddiOS(ios => ios
                .WillTerminate((app) => holder.Shutdown()));
#endif
#if WINDOWS
            events.AddWindows(windows => windows
                .OnClosed((window, args) =>
                {
                    //only shutdown if there are no windows open
                    if (Application.Current?.Windows is [])
                    {
                        holder.Shutdown();
                    }
                }));
#endif
        });

        var app = builder.Build();
        holder.App = app;
        var logger = app.Services.GetRequiredService<ILogger<MauiApp>>();
        logger.LogInformation("App started, {Version}", AppVersion.Version);
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
