using System.Runtime.InteropServices;

namespace FwLiteDesktop;

public static class WindowsKernel
{
    public static void AddFwLiteWindows(this IServiceCollection services)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        if (FwLiteDesktopKernel.IsPackagedApp)
        {
            services.AddSingleton<IMauiInitializeService, AppUpdateService>();
            services.AddSingleton<IMauiInitializeService, WindowsShortcutService>();
        }
    }
}
