using FwLiteDesktop.ServerBridge;
using LcmCrdt;
using LocalWebApp.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;

namespace FwLiteDesktop;

public static class FwLiteDesktopKernel
{
    public static void AddFwLiteDesktopServices(this IServiceCollection services,
        ConfigurationManager configuration,
        ILoggingBuilder logging)
    {
        services.AddSingleton<MainPage>();

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
        //using a lambda here means that the serverManager will be disposed when the app is disposed
        services.AddSingleton<ServerManager>(_ => serverManager);
        services.AddSingleton<IMauiInitializeService>(_ => _.GetRequiredService<ServerManager>());
        configuration.Add<ServerConfigSource>(source => source.ServerManager = serverManager);
        services.AddOptions<LocalWebAppConfig>().BindConfiguration("LocalWebApp");
        logging.AddFile(Path.Combine(FileSystem.AppDataDirectory, "app.log"));
#if DEBUG
        logging.AddDebug();
#endif
    }
}
