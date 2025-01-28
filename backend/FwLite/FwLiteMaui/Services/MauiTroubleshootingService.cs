using FwLiteShared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace FwLiteMaui.Services;

public class MauiTroubleshootingService(IOptions<FwLiteMauiConfig> config, ILogger<MauiTroubleshootingService> logger) : ITroubleshootingService
{
    private readonly ILauncher _launcher = Launcher.Default;
    private readonly IBrowser _browser = Browser.Default;
    private FwLiteMauiConfig Config => config.Value;

    [JSInvokable]
    public async Task<bool> TryOpenDataDirectory()
    {
        try
        {
            //obviously intended to open a url in the browser, but on windows it just opens explorer
            await _browser.OpenAsync("file://" + Config.BaseDataDir);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to open data directory");
            return false;
        }
    }

    [JSInvokable]
    public Task<string> GetDataDirectory()
    {
        return Task.FromResult(Config.BaseDataDir);
    }

    [JSInvokable]
    public async Task OpenLogFile()
    {
        await _launcher.OpenAsync(new OpenFileRequest("Log File", new FileResult(Config.AppLogFilePath)));
    }
}
