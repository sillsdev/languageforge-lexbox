using System.Net;
using System.Text.Json;
using LexCore.Exceptions;
using LexCore.Sync;

namespace LexBoxApi.Services;

public class FwHeadlessClient(HttpClient httpClient, ILogger<FwHeadlessClient> logger)
{
    public async Task SyncMercurialAndHarmony(Guid projectId)
    {
        var response = await httpClient.PostAsync($"/api/merge/execute?projectId={projectId}", null);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to sync Mercurial and Harmony: {StatusCode} {StatusDescription}, projectId: {ProjectId}",
                response.StatusCode,
                response.ReasonPhrase,
                projectId);
            throw await FailedResponse(response);
        }
    }

    /// <summary>
    /// Which of the projects have a headless harmony project, so the lexicon API can serve them.
    /// </summary>
    public async Task<Guid[]> ReadyLexiconProjects(Guid[] projectIds)
    {
        var response = await httpClient.PostAsJsonAsync("/api/lexicon/projects/ready", projectIds);
        if (!response.IsSuccessStatusCode) throw await FailedResponse(response);
        return await response.Content.ReadFromJsonAsync<Guid[]>() ?? [];
    }

    public async Task<SyncJobResult> AwaitStatus(Guid projectId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/merge/await-finished?projectId={projectId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to get sync status: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
                response.StatusCode,
                response.ReasonPhrase,
                projectId,
                responseBody);
            return new SyncJobResult(SyncJobStatusEnum.UnknownError, responseBody);
        }
        var jobResult = await response.Content.ReadFromJsonAsync<SyncJobResult>(cancellationToken);
        if (jobResult is null)
        {
            logger.LogError("Sync status was not a valid SyncJobResult");
            return new SyncJobResult(SyncJobStatusEnum.UnknownError, "Sync status failed to return a result");
        }

        return jobResult;
    }

    public async Task<ProjectSyncStatus> CrdtSyncStatus(Guid projectId)
    {
        var response = await httpClient.GetAsync($"/api/merge/status?projectId={projectId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ProjectSyncStatus>()
                ?? throw new FwHeadlessException(HttpStatusCode.BadGateway, "FwHeadless returned no sync status");
        logger.LogError("Failed to get CRDT sync status: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
            response.StatusCode,
            response.ReasonPhrase,
            projectId,
            await response.Content.ReadAsStringAsync());
        throw await FailedResponse(response);
    }

    public async Task DeleteRepo(Guid projectId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/manage/repo/{projectId}", cancellationToken);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
            return;
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new ProjectSyncInProgressException(projectId);
        logger.LogError("Failed to delete repo: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
            response.StatusCode,
            response.ReasonPhrase,
            projectId,
            await response.Content.ReadAsStringAsync(cancellationToken));
        throw await FailedResponse(response, cancellationToken);
    }

    public async Task DeleteProject(Guid projectId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/manage/project/{projectId}", cancellationToken);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
            return;
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new ProjectSyncInProgressException(projectId);
        logger.LogError("Failed to delete project: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
            response.StatusCode,
            response.ReasonPhrase,
            projectId,
            await response.Content.ReadAsStringAsync(cancellationToken));
        throw await FailedResponse(response, cancellationToken);
    }

    public async Task RegenerateProjectSnapshot(Guid projectId, Guid? commitId = null, bool preserveAllFieldWorksCommits = false)
    {
        var url = $"/api/merge/regenerate-snapshot?projectId={projectId}";
        if (commitId.HasValue)
        {
            url += $"&commitId={commitId.Value}";
            if (preserveAllFieldWorksCommits)
                url += "&preserveAllFieldWorksCommits=true";
        }
        var response = await httpClient.PostAsync(url, null);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed to regenerate project snapshot: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
                response.StatusCode,
                response.ReasonPhrase,
                projectId,
                responseBody);
            throw await FailedResponse(response);
        }
    }

    internal async Task SyncHarmony(Guid projectId)
    {
        var response = await httpClient.PostAsync($"/api/merge/sync-harmony?projectId={projectId}", null);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed to sync Harmony: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
                response.StatusCode,
                response.ReasonPhrase,
                projectId,
                responseBody);
            throw await FailedResponse(response);
        }
    }

    public async Task BlockProject(Guid projectId, string? reason = null)
    {
        var url = $"/api/merge/block?projectId={projectId}";
        if (!string.IsNullOrEmpty(reason))
            url += $"&reason={Uri.EscapeDataString(reason)}";

        var response = await httpClient.PostAsync(url, null);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed to block project: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
                response.StatusCode,
                response.ReasonPhrase,
                projectId,
                responseBody);
            throw await FailedResponse(response);
        }
    }

    public async Task UnblockProject(Guid projectId)
    {
        var url = $"/api/merge/unblock?projectId={projectId}";
        var response = await httpClient.PostAsync(url, null);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed to unblock project: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
                response.StatusCode,
                response.ReasonPhrase,
                projectId,
                responseBody);
            throw await FailedResponse(response);
        }
    }

    public async Task<SyncBlockStatus> GetBlockStatus(Guid projectId)
    {
        var url = $"/api/merge/block-status?projectId={projectId}";
        var response = await httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<SyncBlockStatus>()
                ?? throw new FwHeadlessException(HttpStatusCode.BadGateway, "FwHeadless returned no block status");

        logger.LogError("Failed to get block status: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
            response.StatusCode,
            response.ReasonPhrase,
            projectId,
            await response.Content.ReadAsStringAsync());
        throw await FailedResponse(response);
    }

    private static async Task<FwHeadlessException> FailedResponse(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        return new FwHeadlessException(response.StatusCode, ReadDetail(await response.Content.ReadAsStringAsync(cancellationToken)));
    }

    /// <summary>
    /// The message in a FwHeadless error body: a problem+json detail (or title), a JSON string such as a NotFound&lt;string&gt;, or plain text.
    /// </summary>
    private static string? ReadDetail(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            if (root.ValueKind == JsonValueKind.String) return root.GetString();
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String) return detail.GetString();
                if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String) return title.GetString();
            }
        }
        catch (JsonException)
        {
            // plain text
        }
        return body;
    }
}
