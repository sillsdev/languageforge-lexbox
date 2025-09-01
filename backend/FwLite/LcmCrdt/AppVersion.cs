using System.Reflection;

namespace LcmCrdt;

public class AppVersion
{
    public static readonly string Version = (typeof(AppVersion).Assembly)
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";
}
