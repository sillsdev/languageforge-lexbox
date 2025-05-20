using System.Diagnostics;
using System.Reflection;

namespace FwLiteMaui;

public class FwLiteMauiConfig
{
    public FwLiteMauiConfig()
    {
        var defaultDataPath = FwLiteMauiKernel.IsPortableApp ? Directory.GetCurrentDirectory() : FileSystem.AppDataDirectory;
        //when launching from a notification, the current directory may be C:\Windows\System32, so we'll use the path of the executable instead
        if (defaultDataPath.StartsWith("C:\\Windows\\System32", StringComparison.OrdinalIgnoreCase))
            defaultDataPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) ?? ".";
        BaseDataDir = defaultDataPath;
    }

    public string BaseDataDir
    {
        get;
        set => field = Path.GetFullPath(value);
    }

    public string AppLogFilePath => Path.Combine(BaseDataDir, "app.log");
    public string AuthCacheFilePath => Path.Combine(BaseDataDir, "msal.cache");
}
