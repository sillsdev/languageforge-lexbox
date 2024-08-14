using System.Runtime.InteropServices;

namespace FwDataMiniLcmBridge;

public class FwDataBridgeConfig
{
    private static string UnixDataFolder =>
        Environment.GetEnvironmentVariable("XDG_DATA_HOME") ??
        Path.Join(Environment.GetEnvironmentVariable("HOME") ?? "", ".local", "share");

    private static readonly string DataFolder =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\ProgramData\SIL\FieldWorks"
            : Path.Join(UnixDataFolder, "fieldworks");
    public string ProjectsFolder { get; set; } = Path.Join(DataFolder, "Projects");
    public string TemplatesFolder { get; set; } = Path.Join(DataFolder, "Templates");
}
