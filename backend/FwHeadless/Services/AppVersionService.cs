using System.Reflection;

namespace FwHeadless.Services;

public static class AppVersionService
{
    public static readonly string Version = typeof(AppVersionService).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";
}
