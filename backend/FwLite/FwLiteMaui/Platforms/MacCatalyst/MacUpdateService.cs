using System.Diagnostics;
using FwLiteShared.AppUpdate;
using FwLiteShared.Events;
using LexCore.Entities;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace FwLiteMaui;

/// <summary>
/// Installs FW Lite Mac updates with Velopack. Lexbox's should-update check (<see cref="UpdateChecker"/>)
/// still decides whether an update is offered; this service then fetches the Velopack package from the
/// GitHub releases (<see cref="GithubSource"/>), stages it, and applies it on restart.
///
/// Only a Velopack-packed bundle can update itself. Dev builds and apps installed from a DMG made before
/// Velopack report <see cref="SupportsAutoUpdate"/> false, so the user gets the Download prompt instead.
/// </summary>
public sealed class MacUpdateService(ILogger<MacUpdateService> logger, GlobalEventBus eventBus) : IPlatformUpdateService
{
    private const string RepoUrl = "https://github.com/sillsdev/languageforge-lexbox";

    private readonly Lazy<UpdateManager?> _updateManager = new(() => CreateUpdateManager(logger));
    private VelopackAsset? _stagedUpdate;

    private static UpdateManager? CreateUpdateManager(ILogger logger)
    {
        try
        {
            return new UpdateManager(new GithubSource(RepoUrl, accessToken: null, prerelease: false));
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Velopack is unavailable; Mac updates must be installed manually");
            return null;
        }
    }

    public bool SupportsAutoUpdate => _updateManager.Value?.IsInstalled == true;

    // macOS can tell us whether the connection is expensive (NWPath.IsExpensive), but we don't ask yet.
    public bool IsOnMeteredConnection() => false;

    // Only called on a metered connection, which we never report.
    public Task<bool> RequestPermissionToUpdate(FwLiteRelease latestRelease) => Task.FromResult(true);

    public async Task<UpdateResult> ApplyUpdate(FwLiteRelease latestRelease)
    {
        var updateManager = _updateManager.Value;
        if (updateManager is not { IsInstalled: true }) return UpdateResult.ManualUpdateRequired;
        try
        {
            var update = await updateManager.CheckForUpdatesAsync();
            if (update is null)
            {
                //Lexbox and the Velopack feed disagree, e.g. a second release on the same day shares the Mac
                //version of the first. The DMG download still gets the user the newer build.
                logger.LogWarning("Lexbox offered {Version} but the Velopack feed has nothing newer than {CurrentVersion}",
                    latestRelease.Version, updateManager.CurrentVersion);
                return UpdateResult.ManualUpdateRequired;
            }

            var target = update.TargetFullRelease;
            logger.LogInformation("Downloading update {Version} ({Size} bytes)", target.Version, target.Size);
            var startTimestamp = Stopwatch.GetTimestamp();
            await updateManager.DownloadUpdatesAsync(update, percent =>
            {
                var bytesDownloaded = target.Size * percent / 100;
                var elapsedSeconds = Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds;
                var bytesPerSecond = elapsedSeconds > 0 ? bytesDownloaded / elapsedSeconds : 0;
                eventBus.PublishEvent(new AppUpdateProgressEvent(bytesDownloaded, bytesPerSecond, latestRelease));
            });
            _stagedUpdate = target;
            logger.LogInformation("Update {Version} downloaded; restart to apply", target.Version);
            return UpdateResult.Success;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to download the update");
            return UpdateResult.Failed;
        }
    }

    public Task RestartToApplyUpdate()
    {
        var updateManager = _updateManager.Value;
        var update = _stagedUpdate ?? updateManager?.UpdatePendingRestart;
        if (updateManager is null || update is null)
        {
            logger.LogError("Restart requested but no update has been downloaded");
            return Task.CompletedTask;
        }

        logger.LogInformation("Applying update {Version} and restarting", update.Version);
        //On success Velopack exits this process, so this does not return
        updateManager.ApplyUpdatesAndRestart(update);
        return Task.CompletedTask;
    }
}
