using FwLiteShared.Services;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace FwLiteMaui.Services;

public class MauiTroubleshootingService(IOptions<FwLiteMauiConfig> config) : ITroubleshootingService
{
    private readonly ILauncher _launcher = Launcher.Default;
    private FwLiteMauiConfig Config => config.Value;

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
