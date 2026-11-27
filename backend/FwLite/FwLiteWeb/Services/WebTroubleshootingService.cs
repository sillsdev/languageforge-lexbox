using System.Diagnostics;
using FwLiteShared.Services;
using LcmCrdt;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace FwLiteWeb.Services;

public class WebTroubleshootingService(
    IOptions<LcmCrdtConfig> crdtConfig,
    IOptions<FwLiteWebConfig> webConfig,
    CrdtProjectsService projectsService) : ITroubleshootingService
{
    [JSInvokable]
    public Task<bool> GetCanShare() => Task.FromResult(false);

    [JSInvokable]
    public Task<string> GetDataDirectory() => Task.FromResult(crdtConfig.Value.ProjectPath);

    [JSInvokable]
    public Task<bool> TryOpenDataDirectory() => Task.FromResult(TryOpenDirectory(crdtConfig.Value.ProjectPath));

    [JSInvokable]
    public Task<string> GetAppDirectory() => Task.FromResult(AppDirectory);

    [JSInvokable]
    public Task<bool> TryOpenAppDirectory() => Task.FromResult(TryOpenDirectory(AppDirectory));

    private string AppDirectory => string.IsNullOrEmpty(webConfig.Value.LogFileName)
        ? AppContext.BaseDirectory
        : Path.GetDirectoryName(Path.GetFullPath(webConfig.Value.LogFileName)) ?? AppContext.BaseDirectory;

    private static bool TryOpenDirectory(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            return true;
        }
        catch
        {
            return false;
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

    //web is configured through appsettings.json, there is no user-facing settings file to toggle
    [JSInvokable]
    public Task<bool> GetCanManageSettingsFile() => Task.FromResult(false);

    public Task<bool> GetVerboseLogging() => throw new NotSupportedException();
    public Task SetVerboseLogging(bool enabled) => throw new NotSupportedException();
    public Task<string> GetSettingsFile() => throw new NotSupportedException();
    public Task SetSettingsFile(string contents) => throw new NotSupportedException();
    public Task ShareCrdtProject(string projectCode) => throw new NotSupportedException();

    [JSInvokable]
    public Task RegenerateHarmonySnapshots(string projectCode) =>
        projectsService.RegenerateHarmonySnapshotsAsync(projectCode);

    [JSInvokable]
    public Task RegenerateEntrySearchTable(string projectCode) =>
        projectsService.RegenerateEntrySearchTableAsync(projectCode);
}
