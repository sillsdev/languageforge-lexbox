using System.Reflection;

namespace FwLiteShared.Services;

public class VersionHelper
{
    public static string DisplayVersion(Assembly assembly)
    {
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        //info version may look like v2024-12-12-3073dd1c+3073dd1ce2ff5510f54a9411366f55c958b9ea45. We want to strip off everything after the +, so we can compare versions
        if (infoVersion is not null && infoVersion.Contains('+'))
        {
            infoVersion = infoVersion[..infoVersion.IndexOf('+')];
        }

        return infoVersion ?? "dev";
    }
}
