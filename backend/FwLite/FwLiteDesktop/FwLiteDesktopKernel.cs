using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using FwLiteDesktop.ServerBridge;
using FwLiteShared.Auth;
using LcmCrdt;
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
        configuration.AddJsonFile("appsettings.json", optional: true);

        string environment = "Production";
#if DEBUG
        environment = "Development";
#endif

        var defaultDataPath = IsPackagedApp ? FileSystem.AppDataDirectory : Directory.GetCurrentDirectory();
        var baseDataPath = Path.GetFullPath(configuration.GetSection("FwLiteDesktop").GetValue<string>("BaseDataDir") ?? defaultDataPath);
        Directory.CreateDirectory(baseDataPath);
        var serverManager = new ServerManager(environment, webAppBuilder =>
        {
            webAppBuilder.Logging.AddFile(Path.Combine(baseDataPath, "web-app.log"));
            webAppBuilder.Services.Configure<LcmCrdtConfig>(config =>
            {
                config.ProjectPath = baseDataPath;
            });
            webAppBuilder.Services.Configure<AuthConfig>(config =>
            {
                config.CacheFileName = Path.Combine(baseDataPath, "msal.cache");
                config.SystemWebViewLogin = true;
            });
        });
        //using a lambda here means that the serverManager will be disposed when the app is disposed
        services.AddSingleton<ServerManager>(_ => serverManager);
        services.AddSingleton<IMauiInitializeService>(_ => _.GetRequiredService<ServerManager>());
        services.AddHttpClient();


        services.AddSingleton<IHostEnvironment>(_ => _.GetRequiredService<ServerManager>().WebServices.GetRequiredService<IHostEnvironment>());
        services.AddSingleton<IPreferences>(Preferences.Default);
        services.AddSingleton<IVersionTracking>(VersionTracking.Default);
        services.AddSingleton<IConnectivity>(Connectivity.Current);
        configuration.Add<ServerConfigSource>(source => source.ServerManager = serverManager);
        services.AddOptions<LocalWebAppConfig>().BindConfiguration("LocalWebApp");
        logging.AddFile(Path.Combine(baseDataPath, "app.log"));
        logging.AddConsole();
#if DEBUG
        logging.AddDebug();
#endif
    }

    static readonly Lazy<bool> IsPackagedAppLazy = new(static () =>
    {
        try
        {
            if (Package.Current != null)
                return true;
        }
        catch
        {
            // no-op
        }

        return false;
    });

    public static bool IsPackagedApp => IsPackagedAppLazy.Value;
}
