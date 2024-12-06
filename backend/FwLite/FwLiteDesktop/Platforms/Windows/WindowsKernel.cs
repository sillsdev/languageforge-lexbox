using System.Runtime.InteropServices;

namespace FwLiteDesktop;

public static class WindowsKernel
{
    public static void AddFwLiteWindows(this MauiAppBuilder builder)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        if (FwLiteDesktopKernel.IsPackagedApp)
        {
            builder.Services.AddSingleton<IMauiInitializeService, AppUpdateService>();
            builder.Services.AddSingleton<IMauiInitializeService, WindowsShortcutService>();
        }
    }
}
