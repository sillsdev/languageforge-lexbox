using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace FwDataMiniLcmBridge;

public class FwDataBridgeConfig
{
    private static string UnixDataFolder =>
        Environment.GetEnvironmentVariable("XDG_DATA_HOME") ??
        Path.Join(Environment.GetEnvironmentVariable("HOME") ?? "", ".local", "share");

    private static string UnixProgramFolder => UnixDataFolder;

    [SupportedOSPlatform("windows")]
    private static string WindowsDataFolder =>
        (string?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\SIL\FieldWorks\9", "ProjectsDir", null)
        ?? (string?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\SIL\FieldWorks\9", "ProjectsDir", null)
        ?? @"C:\ProgramData\SIL\FieldWorks\Projects";

    [SupportedOSPlatform("windows")]
    private static string WindowsProgramFolder =>
        (string?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\SIL\FieldWorks\9", "RootCodeDir", null)
        ?? (string?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\SIL\FieldWorks\9", "RootCodeDir", null)
        ?? @"C:\Program Files\SIL\FieldWorks 9";

    private static readonly string DataFolder =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? WindowsDataFolder
            : Path.Join(UnixDataFolder, "fieldworks", "Projects");

    private static readonly string ProgramFolder =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? WindowsProgramFolder
            : Path.Join(UnixProgramFolder, "fieldworks");

    public string ProjectsFolder { get; set; } = DataFolder;
    public string TemplatesFolder { get; set; } = Path.Join(ProgramFolder, "Templates");
    public bool AutoMigrateLcmData { get; set; } = false;
}
