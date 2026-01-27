namespace FwHeadless.Services;

public interface ISyncJobStatusService
{
    void StartSyncing(Guid projectId);
    void StopSyncing(Guid projectId);
    SyncJobStatus SyncStatus(Guid projectId);
}
