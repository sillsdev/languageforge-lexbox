using FwLiteShared.Services;
using LcmCrdt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace FwLiteMaui.Services;

public class MauiTroubleshootingService(
    IOptions<FwLiteMauiConfig> config,
    ILogger<MauiTroubleshootingService> logger,
    CrdtProjectsService projectsService,
    ILauncher launcher,
    IBrowser browser,
    IShare share) : ITroubleshootingService
{
    private readonly ILauncher _launcher = launcher;
    private readonly IBrowser _browser = browser;
    private readonly IShare _share = share;
    private FwLiteMauiConfig Config => config.Value;

    [JSInvokable]
    public Task<bool> GetCanShare() => Task.FromResult(true);

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

    [JSInvokable]
    public async Task ShareLogFile()
    {
        if (File.Exists(Config.AppLogAlternateFilePath))
        {
            var mainLogFile = new ShareFile(Config.AppLogFilePath, "text/plain");
            var secondLogFile = new ShareFile(Config.AppLogAlternateFilePath, "text/plain");
            var shareRequest = new ShareMultipleFilesRequest("FieldWorks Lite logs", [mainLogFile, secondLogFile])
            {
                PresentationSourceBounds = PresentationSourceBounds()
            };
            await _share.RequestAsync(shareRequest);
        }
        else
        {
            var shareRequest =
                new ShareFileRequest("FieldWorks Lite logs", new ShareFile(Config.AppLogFilePath, "text/plain"))
                {
                    PresentationSourceBounds = PresentationSourceBounds()
                };
            await _share.RequestAsync(shareRequest);
        }
    }

    // iPadOS presents the share sheet as a popover anchored to this rectangle; without a source rect
    // UIKit throws. The request originates from a WebView button whose screen position we don't have,
    // so anchor to the centre of the display. Ignored on platforms that don't use a popover.
    private static Rect PresentationSourceBounds()
    {
        var display = DeviceDisplay.Current.MainDisplayInfo;
        var width = display.Width / display.Density;
        var height = display.Height / display.Density;
        return new Rect(width / 2, height / 2, 0, 0);
    }

    [JSInvokable]
    public async Task ShareCrdtProject(string projectCode)
    {
        var crdtProject = projectsService.GetProject(projectCode);
        if (crdtProject is null) throw new ArgumentException($"Project {projectCode} not found");
        var filePath = crdtProject.DbPath;
        var shareTitle = $"FieldWorks Lite project {projectCode}";
        await _share.RequestAsync(new ShareFileRequest(shareTitle, new ShareFile(filePath, "application/x-sqlite3"))
        {
            PresentationSourceBounds = PresentationSourceBounds()
        });
    }

    [JSInvokable]
    public Task RegenerateHarmonySnapshots(string projectCode) =>
        projectsService.RegenerateHarmonySnapshotsAsync(projectCode);

    [JSInvokable]
    public Task RegenerateEntrySearchTable(string projectCode) =>
        projectsService.RegenerateEntrySearchTableAsync(projectCode);
}
