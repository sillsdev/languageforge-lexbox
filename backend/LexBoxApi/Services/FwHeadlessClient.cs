using System.Net;
using System.Text.Json;
using LexCore.Exceptions;
using LexCore.Sync;
using Microsoft.AspNetCore.Mvc;

namespace LexBoxApi.Services;

public class FwHeadlessClient(HttpClient httpClient, ILogger<FwHeadlessClient> logger)
{
    public async Task<(bool success, HttpStatusCode statusCode, string? error)> SyncMercurialAndHarmony(Guid projectId)
    {
        var response = await httpClient.PostAsync($"/api/merge/execute?projectId={projectId}", null);
        string? error = null;
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to sync Mercurial and Harmony: {StatusCode} {StatusDescription}, projectId: {ProjectId}",
                response.StatusCode,
                response.ReasonPhrase,
                projectId);
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(await response.Content.ReadAsStringAsync());
            error = problemDetails?.Detail;
        }

        return (response.IsSuccessStatusCode, response.StatusCode, error);
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

    public async Task<ProjectSyncStatus?> CrdtSyncStatus(Guid projectId)
    {
        var response = await httpClient.GetAsync($"/api/merge/status?projectId={projectId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ProjectSyncStatus>();
        logger.LogError("Failed to get CRDT sync status: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
            response.StatusCode,
            response.ReasonPhrase,
            projectId,
            await response.Content.ReadAsStringAsync());
        return null;
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
        throw new InvalidOperationException($"Failed to delete repo {projectId}: {response.StatusCode} {response.ReasonPhrase}");
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
        throw new InvalidOperationException($"Failed to delete project {projectId}: {response.StatusCode} {response.ReasonPhrase}");
    }

    public async Task<string?> RegenerateProjectSnapshot(Guid projectId)
    {
        var response = await httpClient.PostAsync($"/api/merge/regenerate-snapshot?projectId={projectId}", null);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed to regenerate project snapshot: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
                response.StatusCode,
                response.ReasonPhrase,
                projectId,
                responseBody);
            return responseBody;
        }

        return null;
    }

    internal async Task<string?> SyncHarmony(Guid projectId)
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
            return responseBody;
        }

        return null;
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

            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new NotFoundException($"Project not found in FwHeadless: {projectId}", "Project");

            throw new InvalidOperationException($"Failed to block project: {response.StatusCode} {response.ReasonPhrase}");
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

            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new NotFoundException($"Project not found in FwHeadless: {projectId}", "Project");

            throw new InvalidOperationException($"Failed to unblock project: {response.StatusCode} {response.ReasonPhrase}");
        }
    }

    public async Task<SyncBlockStatus?> GetBlockStatus(Guid projectId)
    {
        var url = $"/api/merge/block-status?projectId={projectId}";
        var response = await httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<SyncBlockStatus>();

        logger.LogError("Failed to get block status: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
            response.StatusCode,
            response.ReasonPhrase,
            projectId,
            await response.Content.ReadAsStringAsync());
        return null;
    }

}
