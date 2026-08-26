using System.Net.Http.Json;
using LexCore.Sync;
using Testing.Fixtures;
using Testing.Services;

namespace Testing.FwHeadless;

public static class FwHeadlessTestHelpers
{
    public static async Task<Guid> CopyProjectToNewProject(HttpClient httpClient, string newProjectCode, string existingProjectCode)
    {
        var result = await httpClient.PostAsync(
            $"api/Testing/copyToNewProject?newProjectCode={newProjectCode}&existingProjectCode={existingProjectCode}",
            null);
        result.EnsureSuccessStatusCode();
        await Utils.WaitForHgRefreshIntervalAsync();
        return await result.Content.ReadFromJsonAsync<Guid>();
    }

    public static async Task TriggerSync(HttpClient httpClient, Guid projectId)
    {
        var result = await httpClient.PostAsync($"api/fw-lite/sync/trigger/{projectId}", null);
        result.ShouldBeSuccessful();
    }

    public static async Task CancelSync(HttpClient httpClient, Guid projectId)
    {
        var result = await httpClient.PostAsync($"api/fw-lite/sync/cancel/{projectId}", null);
        result.ShouldBeSuccessful();
    }

    public static async Task<SyncJobResult?> AwaitSyncFinished(HttpClient httpClient, Guid projectId)
    {
        var syncResult = await AwaitSyncResult(httpClient, projectId);
        if (syncResult?.Status != SyncJobStatusEnum.Success)
            Assert.Fail($"Sync failed with status: {syncResult?.Status}, Error: {syncResult?.Error}");
        return syncResult;
    }

    /// <summary>
    /// Awaits sync completion and returns the <see cref="SyncJobResult"/> regardless of status, unlike
    /// <see cref="AwaitSyncFinished"/> which <c>Assert.Fail</c>s on any non-success status. A repro that
    /// asserts the desired post-fix status itself (so the FluentAssertions failure can surface the actual
    /// error while the fix is still pending) must use this instead of the asserting helper.
    /// </summary>
    public static async Task<SyncJobResult?> AwaitSyncResult(HttpClient httpClient, Guid projectId)
    {
        var giveUpAt = DateTime.UtcNow + TimeSpan.FromMinutes(4);
        while (giveUpAt > DateTime.UtcNow)
        {
            try
            {
                var result = await httpClient.GetAsync($"api/fw-lite/sync/await-sync-finished/{projectId}", new CancellationTokenSource(TimeSpan.FromSeconds(25)).Token);
                result.EnsureSuccessStatusCode();
                return await result.Content.ReadFromJsonAsync<SyncJobResult>();
            }
            catch (OperationCanceledException)
            {
            }
        }
        Assert.Fail("timed out waiting for sync to finish");
        return null;
    }
}
