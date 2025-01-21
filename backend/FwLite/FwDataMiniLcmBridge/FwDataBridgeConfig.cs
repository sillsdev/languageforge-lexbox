using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FwDataMiniLcmBridge;

public class FwDataBridgeConfig
{
    private static string UnixDataFolder =>
        Environment.GetEnvironmentVariable("XDG_DATA_HOME") ??
        Path.Join(Environment.GetEnvironmentVariable("HOME") ?? "", ".local", "share");

    private static string WindowsDataFolder =>
        (string?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\SIL\FieldWorks\9", "ProjectsDir", null)
        ?? @"C:\ProgramData\SIL\FieldWorks";

    private static readonly string DataFolder =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? WindowsDataFolder
            : Path.Join(UnixDataFolder, "fieldworks");
    public string ProjectsFolder { get; set; } = Path.Join(DataFolder, "Projects");
    public string TemplatesFolder { get; set; } = Path.Join(DataFolder, "Templates");
}
