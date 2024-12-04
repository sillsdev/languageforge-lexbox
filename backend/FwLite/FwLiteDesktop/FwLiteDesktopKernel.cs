using Windows.ApplicationModel;
using FwLiteShared;
using FwLiteShared.Auth;
using LcmCrdt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
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
        services.AddFwLiteShared(new HostingEnvironment() { EnvironmentName = environment });
        var defaultDataPath = IsPackagedApp ? FileSystem.AppDataDirectory : Directory.GetCurrentDirectory();
        var baseDataPath = Path.GetFullPath(configuration.GetSection("FwLiteDesktop").GetValue<string>("BaseDataDir") ?? defaultDataPath);
        Directory.CreateDirectory(baseDataPath);
        services.Configure<LcmCrdtConfig>(config =>
        {
            config.ProjectPath = baseDataPath;
        });
        services.Configure<AuthConfig>(config =>
        {
            config.CacheFileName = Path.Combine(baseDataPath, "msal.cache");
            config.SystemWebViewLogin = true;
        });
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
