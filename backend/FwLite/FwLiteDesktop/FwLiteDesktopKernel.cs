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

        services.Configure<AuthConfig>(config =>
            config.LexboxServers =
            [
                new(new("https://lexbox.dev.languagetechnology.org"), "Lexbox Dev"),
                new(new("https://staging.languagedepot.org"), "Lexbox Staging")
            ]);
        services.Configure<AuthConfig>(c => c.ClientId = "becf2856-0690-434b-b192-a4032b72067f");

        string environment = "Production";
#if DEBUG
        environment = "Development";
#endif
        var env = new HostingEnvironment() { EnvironmentName = environment };
        services.AddSingleton<IHostEnvironment>(env);
        services.AddFwLiteShared(env);
        var defaultDataPath = IsPackagedApp ? FileSystem.AppDataDirectory : Directory.GetCurrentDirectory();
        var baseDataPath = Path.GetFullPath(configuration.GetSection("FwLiteDesktop").GetValue<string>("BaseDataDir") ??
                                            defaultDataPath);
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
        // logging.AddFile(Path.Combine(baseDataPath, "app.log"));
        logging.AddConsole();
#if DEBUG
        logging.AddDebug();
#endif
    }

#if WINDOWS
    private static readonly Lazy<bool> IsPackagedAppLazy = new(static () =>
    {
        try
        {
            if (Windows.ApplicationModel.Package.Current != null)
                return true;
            return false;
        }
        catch
        {
            // no-op
        }

        return false;
    });

    public static bool IsPackagedApp => IsPackagedAppLazy.Value;
#else
    private static bool IsPackagedApp => true;
#endif
}
