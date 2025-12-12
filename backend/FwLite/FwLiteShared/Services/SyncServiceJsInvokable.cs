using FwLiteShared.Auth;
using FwLiteShared.Sync;
using LcmCrdt.Data;
using LexCore.Sync;
using SIL.Harmony;
using Microsoft.JSInterop;

namespace FwLiteShared.Services;

public class SyncServiceJsInvokable(SyncService syncService, SyncRepository syncRepository)
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
        return syncService.ExecuteSync(skipNotifications);
    }

    [JSInvokable]
    public Task<LexboxServer?> GetCurrentServer()
    {
        return syncService.GetCurrentServer();
    }
}
