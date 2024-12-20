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

        string environment = "Production";
#if DEBUG
        environment = "Development";
        services.AddBlazorWebViewDeveloperTools();
#endif
        var env = new HostingEnvironment() { EnvironmentName = environment };
        services.AddSingleton<IHostEnvironment>(env);
        services.AddFwLiteShared(env);
        services.AddMauiBlazorWebView();
        services.AddSingleton<IMauiInitializeService, HostedServiceAdapter>();
#if INCLUDE_FWDATA_BRIDGE
        //need to call them like this otherwise we need a using statement at the top of the file
        FwDataMiniLcmBridge.FwDataBridgeKernel.AddFwDataBridge(services);
        FwLiteProjectSync.FwLiteProjectSyncKernel.AddFwLiteProjectSync(services);
#endif
#if WINDOWS
        services.AddFwLiteWindows();
#endif
#if ANDROID
        services.Configure<AuthConfig>(config => config.ParentActivityOrWindow = Platform.CurrentActivity);
#endif
        services.Configure<AuthConfig>(config => config.AfterLoginWebView = () =>
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window is not null) Application.Current?.ActivateWindow(window);
        });

        var defaultDataPath = IsPortableApp ? Directory.GetCurrentDirectory() : FileSystem.AppDataDirectory;
        var baseDataPath = Path.GetFullPath(configuration.GetSection("FwLiteDesktop").GetValue<string>("BaseDataDir") ??
                                            defaultDataPath);
        logging.AddFilter("FwLiteShared.Auth.LoggerAdapter", LogLevel.Warning);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database", LogLevel.Warning);
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
        services.AddSingleton<IPreferences>(Preferences.Default);
        services.AddSingleton<IVersionTracking>(VersionTracking.Default);
        services.AddSingleton<IConnectivity>(Connectivity.Current);
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

    public static bool IsPortableApp => !IsPackagedAppLazy.Value;
#else
    /// <summary>
    /// indicates that the app is running in portable mode and it should put log files and data in the current directory
    /// </summary>
    public static bool IsPortableApp => false;
#endif

    private class HostedServiceAdapter(IEnumerable<IHostedService> hostedServices, ILogger<HostedServiceAdapter> logger) : IMauiInitializeService, IAsyncDisposable
    {
        private CancellationTokenSource _cts = new();
        public void Initialize(IServiceProvider services)
        {
            logger.LogInformation("Initializing hosted services");
            foreach (var hostedService in hostedServices)
            {
                _ = Task.Run(() => hostedService.StartAsync(_cts.Token), _cts.Token);
            }
        }

        public async ValueTask DisposeAsync()
        {
            //todo this is never called because the service provider is not disposed
            logger.LogInformation("Disposing hosted services");
            foreach (var hostedService in hostedServices)
            {
                await hostedService.StopAsync(_cts.Token);
            }
            await _cts.CancelAsync();
            _cts.Dispose();
        }
    }
}
