using System.Runtime.InteropServices;
using FwLiteMaui.Services;
using FwLiteShared;
using FwLiteShared.AppUpdate;
using FwLiteShared.Auth;
using FwLiteShared.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Maui.Platform;

namespace FwLiteMaui;

public static class WindowsKernel
{

    public static void AddFwLiteWindows(this IServiceCollection services, IHostEnvironment environment)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        services.RemoveAll(typeof(IPlatformUpdateService));
        services.AddSingleton<AppUpdateService>();
        services.AddSingleton<IMauiInitializeService>(s => s.GetRequiredService<AppUpdateService>());
        services.AddSingleton<IPlatformUpdateService>(s => s.GetRequiredService<AppUpdateService>());
        services.AddSingleton<IMultiWindowService, WindowsMultiWindowService>();
        if (!FwLiteMauiKernel.IsPortableApp)
        {
            services.AddSingleton<IMauiInitializeService, WindowsShortcutService>();
        }

        services.Configure<AuthConfig>(config =>
        {
            config.AfterLoginWebView = () =>
            {
                var window = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (window is null) throw new InvalidOperationException("Could not find window");
                //note, window.Activate() does not work per https://github.com/microsoft/microsoft-ui-xaml/issues/7595
                var hwnd = window.GetWindowHandle();
                WindowHelper.SetForegroundWindow(hwnd);
            };
        });

        services.Configure<FwLiteConfig>(config =>
        {
            config.UseDevAssets = environment.IsDevelopment();
        });
    }
}

public class WindowHelper
{
    [DllImport("user32.dll")]
    public static extern void SetForegroundWindow(IntPtr hWnd);
}
