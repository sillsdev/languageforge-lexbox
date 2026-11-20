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

    /// <summary>
    /// App-local state only: logs, the auth cache, the settings file and the WebView profile. On Windows MSIX
    /// this is the package folder, which is deleted on uninstall, so never put user data here.
    /// Projects belong in <see cref="ResolveProjectDataDir"/>.
    /// </summary>
    public string BaseDataDir
    {
        get;
        set => field = Path.GetFullPath(value);
    }

    /// <summary>
    /// Replaces <see cref="ResolveProjectDataDir"/>, e.g. env FwLiteMaui__OverrideProjectDataDir.
    /// </summary>
    public string? OverrideProjectDataDir
    {
        get;
        set => field = string.IsNullOrEmpty(value) ? null : Path.GetFullPath(value);
    }

    /// <summary>
    /// Where projects, their media cache and failed syncs should live. Defaults to <see cref="BaseDataDir"/>,
    /// except on Windows MSIX installs: the package folder is deleted on uninstall, so project data goes to
    /// ProgramData instead (#1391). Overriding <see cref="BaseDataDir"/> moves this along with it.
    /// <para>
    /// This is the intended location, not necessarily the one in use: if migrating existing data here fails, the
    /// app keeps using <see cref="BaseDataDir"/> (see <see cref="ProjectDataLocation"/>). Read the folder in use
    /// from <c>LcmCrdtConfig.ProjectPath</c> or <c>HarmonyConfig</c>, never from this property.
    /// </para>
    /// </summary>
    //a method, not a property: configuration binding reads every settable property and writes the value back,
    //which freezes a computed default at bind time
    public string ResolveProjectDataDir()
    {
        if (OverrideProjectDataDir is not null) return OverrideProjectDataDir;
#if WINDOWS
        if (!FwLiteMauiKernel.IsPortableApp && BaseDataDir == _defaultBaseDataDir)
            return WindowsProjectDataDir();
#endif
        return BaseDataDir;
    }

    /// <summary>
    /// C:\ProgramData\SIL\FieldWorks Lite\{profile folder}, next to FieldWorks' own C:\ProgramData\SIL\FieldWorks.
    /// MSIX only redirects AppData writes, so this folder survives uninstall. One folder per Windows user because
    /// the project db holds per-user state (login, role). Other accounts can read it (ProgramData's default ACL).
    /// Keyed by the C:\Users\{profile folder} name, not the user name: renaming an account keeps its profile
    /// folder, and a local and a domain account with the same name get different ones.
    /// </summary>
    public static string WindowsProjectDataDir()
    {
        var userFolder = Path.GetFileName(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        if (string.IsNullOrEmpty(userFolder)) userFolder = Environment.UserName;
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
    public string SettingsFilePath => Path.Combine(BaseDataDir, FwLiteMauiConfiguration.SettingsFileName);
}
