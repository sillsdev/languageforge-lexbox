namespace FwLiteShared.Services;

public interface ITroubleshootingService
{
    Task<bool> GetCanShare();
    Task<string> GetProcessArchitecture();
    Task<bool> TryOpenDataDirectory();
    Task<string> GetDataDirectory();
    Task OpenLogFile();
    Task ShareLogFile();
    Task ShareCrdtProject(string projectCode);
}
