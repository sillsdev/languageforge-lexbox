using LexCore.Sync;

namespace LexBoxApi.Services;

public class FwHeadlessClient(HttpClient httpClient)
{
    public async Task<SyncResult?> CrdtSync(Guid projectId)
    {
        var response = await httpClient.PostAsync($"/api/crdt-sync?projectId={projectId}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SyncResult>();
    }
}
