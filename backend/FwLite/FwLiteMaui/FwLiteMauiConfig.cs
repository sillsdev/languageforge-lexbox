using System.Diagnostics;
using System.Reflection;

namespace FwLiteMaui;

public class FwLiteMauiConfig
{
    private readonly string _defaultBaseDataDir;

    public FwLiteMauiConfig()
    {
        var defaultDataPath = FwLiteMauiKernel.IsPortableApp ? Directory.GetCurrentDirectory() : FileSystem.AppDataDirectory;
        //when launching from a notification, the current directory may be C:\Windows\System32, so we'll use the path of the executable instead
        if (defaultDataPath.StartsWith("C:\\Windows\\System32", StringComparison.OrdinalIgnoreCase))
            defaultDataPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) ?? ".";
        BaseDataDir = defaultDataPath;
        _defaultBaseDataDir = BaseDataDir;
    }

    public string BaseDataDir
    {
        get;
        set => field = Path.GetFullPath(value);
    }

    /// <summary>
    /// Where projects, their media cache and failed syncs live. Defaults to <see cref="BaseDataDir"/>, except on
    /// Windows MSIX installs: the package folder is deleted on uninstall, so project data goes to ProgramData
    /// instead (#1391). Overriding <see cref="BaseDataDir"/> moves this along with it.
    /// </summary>
    public string ProjectDataDir
    {
        get => field ?? DefaultProjectDataDir();
        set => field = Path.GetFullPath(value);
    }

    private string DefaultProjectDataDir()
    {
#if WINDOWS
        if (!FwLiteMauiKernel.IsPortableApp && BaseDataDir == _defaultBaseDataDir)
            return WindowsProjectDataDir();
#endif
        return BaseDataDir;
    }

    /// <summary>
    /// C:\ProgramData\SIL\FieldWorks Lite\{user}, next to FieldWorks' own C:\ProgramData\SIL\FieldWorks.
    /// MSIX only redirects AppData writes, so this folder survives uninstall. One folder per Windows user: a
    /// project file is only writable by the user that created it, and the project db holds per-user state.
    /// </summary>
    public static string WindowsProjectDataDir()
    {
        var userFolder = string.Concat(Environment.UserName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SIL",
            "FieldWorks Lite",
            userFolder);
    }

    public int MaxLogFileSize { get; set; } = 50 * 1024 * 1024;
    public int MaxLogFileCount { get; set; } = 2;

    public string AppLogFilePath => Path.Combine(BaseDataDir, "app.log");
    public string AppLogAlternateFilePath => Path.Combine(BaseDataDir, "app1.log");
    public string AuthCacheFilePath => Path.Combine(BaseDataDir, "msal.cache");
}
