namespace FwLiteShared.Services;

public interface ITroubleshootingService
{
    Task<bool> GetCanShare();
    Task<bool> TryOpenDataDirectory();
    Task<string> GetDataDirectory();
    /// <summary>where logs and the settings file live, which can differ from the data directory (e.g. Windows MSIX)</summary>
    Task<string> GetAppDirectory();
    Task<bool> TryOpenAppDirectory();
    Task OpenLogFile();
    Task ShareLogFile();
    /// <summary>false when the host has no user-editable settings file (web), which also rules out the verbose logging toggle</summary>
    Task<bool> GetCanManageSettingsFile();
    Task<bool> GetVerboseLogging();
    Task SetVerboseLogging(bool enabled);
    /// <summary>raw contents of the settings file, empty when it doesn't exist</summary>
    Task<string> GetSettingsFile();
    /// <summary>replaces the settings file (deletes it when empty) and applies it; throws on invalid JSON</summary>
    Task SetSettingsFile(string contents);
    Task ShareCrdtProject(string projectCode);
    Task RegenerateHarmonySnapshots(string projectCode);
    Task RegenerateEntrySearchTable(string projectCode);
}
