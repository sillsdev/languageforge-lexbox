﻿using System.Runtime.InteropServices;

namespace FwLiteMaui;

public static class WindowsKernel
{
    public static void AddFwLiteWindows(this IServiceCollection services)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        if (!FwLiteMauiKernel.IsPortableApp)
        {
            services.AddSingleton<IMauiInitializeService, AppUpdateService>();
            services.AddSingleton<IMauiInitializeService, WindowsShortcutService>();
        }
    }
}
