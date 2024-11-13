using System.Reflection;

namespace FwHeadless;

public static class AppVersionService
{
    public static readonly string Version = typeof(AppVersionService).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";
}
