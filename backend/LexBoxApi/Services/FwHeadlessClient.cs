using LexCore.Sync;

namespace LexBoxApi.Services;

public class FwHeadlessClient(HttpClient httpClient, ILogger<FwHeadlessClient> logger)
{
    public async Task<SyncResult?> CrdtSync(Guid projectId)
    {
        var response = await httpClient.PostAsync($"/api/crdt-sync?projectId={projectId}", null);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<SyncResult>();
        logger.LogError("Failed to sync CRDT: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
            response.StatusCode,
            response.ReasonPhrase,
            projectId,
            await response.Content.ReadAsStringAsync());
        return null;
    }

    public async Task<ProjectSyncStatus?> CrdtSyncStatus(Guid projectId)
    {
        var response = await httpClient.PostAsync($"/api/crdt-sync-status?projectId={projectId}", null);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ProjectSyncStatus>();
        logger.LogError("Failed to get CRDT sync status: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
            response.StatusCode,
            response.ReasonPhrase,
            projectId,
            await response.Content.ReadAsStringAsync());
        return null;
    }
}
