using FwLiteDesktop.ServerBridge;
using LcmCrdt;
using LocalWebApp.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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

        string environment = "Production";
#if DEBUG
        environment = "Development";
#endif
        var serverManager = new ServerManager(environment, webAppBuilder =>
        {
            webAppBuilder.Logging.AddFile(Path.Combine(FileSystem.AppDataDirectory, "web-app.log"));
            webAppBuilder.Services.Configure<LcmCrdtConfig>(config =>
            {
                config.ProjectPath = FileSystem.AppDataDirectory;
            });
            webAppBuilder.Services.Configure<AuthConfig>(config =>
            {
                config.CacheFileName = Path.Combine(FileSystem.AppDataDirectory, "msal.cache");
                config.SystemWebViewLogin = true;
            });
        });
        //using a lambda here means that the serverManager will be disposed when the app is disposed
        services.AddSingleton<ServerManager>(_ => serverManager);
        services.AddSingleton<IMauiInitializeService>(_ => _.GetRequiredService<ServerManager>());
        services.AddSingleton<IHostEnvironment>(_ => _.GetRequiredService<ServerManager>().WebServices.GetRequiredService<IHostEnvironment>());
        configuration.Add<ServerConfigSource>(source => source.ServerManager = serverManager);
        services.AddOptions<LocalWebAppConfig>().BindConfiguration("LocalWebApp");
        logging.AddFile(Path.Combine(FileSystem.AppDataDirectory, "app.log"));
        logging.AddConsole();
#if DEBUG
        logging.AddDebug();
#endif
    }
}
