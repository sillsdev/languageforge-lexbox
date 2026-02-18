namespace FwLiteShared.Services;

public interface ITroubleshootingService
{
    bool CanShare { get; }
    Task<bool> TryOpenDataDirectory();
    Task<string> GetDataDirectory();
    Task OpenLogFile();
    Task ShareLogFile();
    Task ShareCrdtProject(string projectCode);
}
