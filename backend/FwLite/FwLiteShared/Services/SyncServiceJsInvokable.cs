using FwLiteShared.Auth;
using FwLiteShared.KeepAwake;
using FwLiteShared.LocalSync;
using FwLiteShared.Sync;
using LcmCrdt.Data;
using LexCore.Sync;
using SIL.Harmony;
using Microsoft.JSInterop;

namespace FwLiteShared.Services;

public class SyncServiceJsInvokable(
    SyncService syncService,
    SyncRepository syncRepository,
    FolderSyncService folderSyncService,
    IKeepAwake keepAwake)
{
    [JSInvokable]
    public Task<ProjectSyncStatus> GetSyncStatus()
    {
        return syncService.GetSyncStatus();
    }

    [JSInvokable]
    public async Task<SyncJobResult> TriggerFwHeadlessSync()
    {
        var triggerResponse = await syncService.TriggerSync();

        // Check if trigger failed
        if (triggerResponse is null)
        {
            return new SyncJobResult(SyncJobStatusEnum.UnableToAuthenticate, "Unable to trigger sync - not authenticated");
        }

        if (!triggerResponse.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessage(triggerResponse);
            var status = triggerResponse.StatusCode switch
            {
                System.Net.HttpStatusCode.Locked => SyncJobStatusEnum.SyncBlocked,
                System.Net.HttpStatusCode.NotFound => SyncJobStatusEnum.ProjectNotFound,
                System.Net.HttpStatusCode.Forbidden => SyncJobStatusEnum.UnableToAuthenticate,
                _ => SyncJobStatusEnum.UnableToSync
            };
            return new SyncJobResult(status, errorMessage);
        }

        return await syncService.AwaitSyncFinished();
    }

    private static async Task<string> ExtractErrorMessage(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            // Try to parse as ProblemDetails JSON
            var problemDetails = System.Text.Json.JsonDocument.Parse(content);
            if (problemDetails.RootElement.TryGetProperty("detail", out var detail))
            {
                return detail.GetString() ?? content;
            }
            if (problemDetails.RootElement.TryGetProperty("title", out var title))
            {
                return title.GetString() ?? content;
            }
            return content;
        }
        catch
        {
            return $"Sync trigger failed with status {(int)response.StatusCode} {response.ReasonPhrase}";
        }
    }

    [JSInvokable]
    public Task<PendingCommits?> CountPendingCrdtCommits()
    {
        return syncService.CountPendingCrdtCommits();
    }

    [JSInvokable]
    public Task<DateTimeOffset?> GetLatestSyncedCommitDate()
    {
        return syncRepository.GetLatestSyncedCommitDate();
    }

    [JSInvokable]
    public Task<SyncResults> ExecuteSync(bool skipNotifications)
    {
        return Task.Run(async () => await syncService.ExecuteSync(skipNotifications));
    }

    [JSInvokable]
    public Task<LexboxServer?> GetCurrentServer()
    {
        return syncService.GetCurrentServer();
    }

    [JSInvokable]
    public async Task<FolderSyncSummary> SyncWithFolder(string path)
    {
        var result = await keepAwake.RunAsync(new("Syncing with a folder"),
            async () => await Task.Run(async () => await folderSyncService.SyncWithFolder(path)));
        return FolderSyncSummary.From(result);
    }

    //Null when the folder is gone (e.g. the drive is unplugged), so the viewer only offers a sync that can work
    [JSInvokable]
    public string? GetRememberedSyncFolder()
    {
        var path = folderSyncService.GetRememberedFolder();
        return path is not null && Directory.Exists(path) ? path : null;
    }
}
