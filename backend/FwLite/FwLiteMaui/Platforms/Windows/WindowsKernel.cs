using System.Runtime.InteropServices;
using FwLiteShared;
using Microsoft.Extensions.Hosting;

namespace FwLiteMaui;

public static class WindowsKernel
{
    public static void AddFwLiteWindows(this IServiceCollection services, IHostEnvironment environment)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        if (!FwLiteMauiKernel.IsPortableApp)
        {
            services.AddSingleton<IMauiInitializeService, AppUpdateService>();
            services.AddSingleton<IMauiInitializeService, WindowsShortcutService>();
        }
        services.Configure<FwLiteConfig>(config =>
        {
            config.UseDevAssets = environment.IsDevelopment();
        });
    }
}
