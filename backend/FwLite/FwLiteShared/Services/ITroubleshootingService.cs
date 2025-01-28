namespace FwLiteShared.Services;

public interface ITroubleshootingService
{
    Task<string> GetDataDirectory();
    Task OpenLogFile();
}
