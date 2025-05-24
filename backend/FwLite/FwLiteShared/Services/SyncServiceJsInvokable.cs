using FwLiteShared.Sync;
using LexCore.Sync;
using SIL.Harmony;
using Microsoft.JSInterop;

namespace FwLiteShared.Services;

public class SyncServiceJsInvokable(SyncService syncService)
{
    [JSInvokable]
    public Task<ProjectSyncStatus?> GetSyncStatus()
    {
        return syncService.GetSyncStatus();
    }

    [JSInvokable]
    public async Task<SyncResult?> TriggerFwHeadlessSync()
    {
        await syncService.TriggerSync();
        return await syncService.AwaitSyncFinished();
    }

    [JSInvokable]
    public Task<SyncResult?> CountPendingCrdtCommits()
    {
        return syncService.CountPendingCrdtCommits();
    }

    [JSInvokable]
    public Task<DateTimeOffset?> GetLatestCommitDate()
    {
        return syncService.GetLatestCommitDate();
    }

    [JSInvokable]
    public Task<SyncResults> ExecuteSync(bool skipNotifications)
    {
        return syncService.ExecuteSync(skipNotifications);
    }
}
