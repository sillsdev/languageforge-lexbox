using LexCore.Sync;

namespace LexBoxApi.Services;

public class FwHeadlessClient(HttpClient httpClient, ILogger<FwHeadlessClient> logger)
{
    public async Task<bool> CrdtSync(Guid projectId)
    {
        var response = await httpClient.PostAsync($"/api/crdt-sync?projectId={projectId}", null);
        if (response.IsSuccessStatusCode)
            return true;
        logger.LogError("Failed to sync CRDT: {StatusCode} {StatusDescription}, projectId: {ProjectId}",
            response.StatusCode,
            response.ReasonPhrase,
            projectId);
        return false;
    }

    public async Task<SyncResult?> AwaitStatus(Guid projectId)
    {
        var response = await httpClient.GetAsync($"/api/await-sync-finished?projectId={projectId}");
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to get sync status: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
                response.StatusCode,
                response.ReasonPhrase,
                projectId,
                await response.Content.ReadAsStringAsync());
            return null;
        }
        var jobResult = await response.Content.ReadFromJsonAsync<SyncJobResult>();
        if (jobResult is null)
        {
            logger.LogError("Failed to get sync status");
            return null;
        }

        if (jobResult.Result == SyncJobResultEnum.Success)
        {
            return jobResult.SyncResult;
        }
        logger.LogError("Sync failed: {JobResult}", jobResult);
        return null;
    }
}
