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
}
