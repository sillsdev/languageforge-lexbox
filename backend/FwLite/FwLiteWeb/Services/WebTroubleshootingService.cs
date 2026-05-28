using System.Diagnostics;
using FwLiteShared.Services;
using LcmCrdt;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace FwLiteWeb.Services;

public class WebTroubleshootingService(
    IOptions<LcmCrdtConfig> crdtConfig,
    IOptions<FwLiteWebConfig> webConfig) : ITroubleshootingService
{
    [JSInvokable]
    public Task<bool> GetCanShare() => Task.FromResult(false);

    [JSInvokable]
    public Task<string> GetDataDirectory()
    {
        return Task.FromResult(crdtConfig.Value.ProjectPath);
    }

    [JSInvokable]
    public Task<bool> TryOpenDataDirectory()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = crdtConfig.Value.ProjectPath,
                UseShellExecute = true
            });
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    [JSInvokable]
    public Task OpenLogFile()
    {
        var logFileName = webConfig.Value.LogFileName;
        if (string.IsNullOrEmpty(logFileName))
            throw new InvalidOperationException("No log file configured");
        Process.Start(new ProcessStartInfo
        {
            FileName = Path.GetFullPath(logFileName),
            UseShellExecute = true
        });
        return Task.CompletedTask;
    }

    public Task ShareLogFile() => throw new NotSupportedException();
    public Task ShareCrdtProject(string projectCode) => throw new NotSupportedException();
}
