using System.Diagnostics;
using System.Reflection;
using FwLiteShared;
using FwLiteShared.Auth;
using LcmCrdt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;

namespace FwLiteMaui;

public static class FwLiteMauiKernel
{
    public static void AddFwLiteMauiServices(this IServiceCollection services,
        ConfigurationManager configuration,
        ILoggingBuilder logging)
    {
        services.AddSingleton<MainPage>();
        configuration.AddJsonFile("appsettings.json", optional: true);

        string environment = "Production";
#if DEBUG
        environment = "Development";
#endif
        IHostEnvironment env = new HostingEnvironment() { EnvironmentName = environment };
        services.AddSingleton<IHostEnvironment>(env);
        services.AddMauiBlazorWebView();
        services.AddBlazorWebViewDeveloperTools();
        //must be added after blazor as it modifies IJSRuntime in order to intercept it's constructor
        services.AddFwLiteShared(env);
        services.AddSingleton<HostedServiceAdapter>();
        services.AddSingleton<IMauiInitializeService>(sp => sp.GetRequiredService<HostedServiceAdapter>());
        services.Configure<AuthConfig>(config =>
        {
            List<LexboxServer> servers =
            [
                new(new("https://staging.languagedepot.org"), "Lexbox Staging")
            ];
            if (env.IsDevelopment())
            {
                servers.Add(new(new("https://lexbox.dev.languagetechnology.org"), "Lexbox Dev"));
            }

            config.LexboxServers = servers.ToArray();
            config.AfterLoginWebView = () =>
            {
                var window = Application.Current?.Windows.FirstOrDefault();
                if (window is not null) Application.Current?.ActivateWindow(window);
            };
        });
#if INCLUDE_FWDATA_BRIDGE
        //need to call them like this otherwise we need a using statement at the top of the file
        FwDataMiniLcmBridge.FwDataBridgeKernel.AddFwDataBridge(services);
        FwLiteProjectSync.FwLiteProjectSyncKernel.AddFwLiteProjectSync(services);
        services.AddSingleton<FwLiteShared.Services.IAppLauncher, FwLiteMaui.Services.AppLauncher>();
#endif
#if WINDOWS
        services.AddFwLiteWindows(env);
#endif
#if ANDROID
        services.Configure<AuthConfig>(config => config.ParentActivityOrWindow = Platform.CurrentActivity);
#endif

        services.Configure<FwLiteConfig>(config =>
        {
            config.AppVersion = AppVersion.Version;
            if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            {
                config.Os = FwLitePlatform.Android;
            }
            else if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
            {
                config.Os = FwLitePlatform.iOS;
            }
            else if (DeviceInfo.Current.Platform == DevicePlatform.macOS)
            {
                config.Os = FwLitePlatform.Mac;
            }
            else if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
            {
                config.Os = FwLitePlatform.Windows;
            }
            else
            {
                config.Os = FwLitePlatform.Other;
            }
        });

        var defaultDataPath = IsPortableApp ? Directory.GetCurrentDirectory() : FileSystem.AppDataDirectory;
        //when launching from a notification, the current directory may be C:\Windows\System32, so we'll use the path of the executable instead
        if (defaultDataPath.StartsWith("C:\\Windows\\System32", StringComparison.OrdinalIgnoreCase))
            defaultDataPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName ?? Assembly.GetExecutingAssembly().Location) ?? ".";
        var baseDataPath = Path.GetFullPath(configuration.GetSection("FwLiteMaui").GetValue<string>("BaseDataDir") ??
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

    internal class HostedServiceAdapter(IEnumerable<IHostedService> hostedServices, ILogger<HostedServiceAdapter> logger) : IMauiInitializeService, IAsyncDisposable
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
            logger.LogInformation("Disposing hosted services");
            //todo this should probably have a timeout so we don't hang forever
            foreach (var hostedService in hostedServices)
            {
                await hostedService.StopAsync(_cts.Token);
            }
            await _cts.CancelAsync();
            _cts.Dispose();
        }
    }
}
