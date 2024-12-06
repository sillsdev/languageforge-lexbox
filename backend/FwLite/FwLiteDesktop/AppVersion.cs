using System.Reflection;

namespace FwLiteDesktop;

public class AppVersion
{
    public static readonly string Version = typeof(AppVersion).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";
}
