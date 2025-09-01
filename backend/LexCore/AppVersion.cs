using System.Reflection;

namespace LexCore;

public static class AppVersion
{
    public static readonly string CoreVersion = Get(typeof(AppVersion));
    public static string Get(Type t) => t.Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";
}
