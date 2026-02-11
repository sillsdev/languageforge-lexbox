namespace FwHeadless.Services;

public interface IProjectMetadataService
{
    Task BlockFromSyncAsync(Guid projectId, string? reason = null);
    Task UnblockFromSyncAsync(Guid projectId);
    Task<SyncBlockedInfo?> GetSyncBlockedInfoAsync(Guid projectId);
}
