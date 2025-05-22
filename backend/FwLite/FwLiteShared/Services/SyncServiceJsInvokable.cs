using FwLiteShared.Sync;
using LexCore.Sync;
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
}
