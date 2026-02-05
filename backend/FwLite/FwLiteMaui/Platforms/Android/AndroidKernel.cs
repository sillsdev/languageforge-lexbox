using FwLiteShared.AppUpdate;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FwLiteMaui;

public static class AndroidKernel
{
    public static void AddFwLiteAndroid(this IServiceCollection services)
    {
        // Replace the default IPlatformUpdateService with the Android implementation
        services.RemoveAll(typeof(IPlatformUpdateService));
        services.AddSingleton<AndroidUpdateService>();
        services.AddSingleton<IMauiInitializeService>(s => s.GetRequiredService<AndroidUpdateService>());
        services.AddSingleton<IPlatformUpdateService>(s => s.GetRequiredService<AndroidUpdateService>());
    }
}
