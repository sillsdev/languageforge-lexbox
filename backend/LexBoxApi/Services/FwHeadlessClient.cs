﻿using System.Net;
using LexCore.Exceptions;
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

    public async Task<SyncJobResult> AwaitStatus(Guid projectId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/await-sync-finished?projectId={projectId}", cancellationToken);
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
        var response = await httpClient.GetAsync($"/api/crdt-sync-status?projectId={projectId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ProjectSyncStatus>();
        logger.LogError("Failed to get CRDT sync status: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
            response.StatusCode,
            response.ReasonPhrase,
            projectId,
            await response.Content.ReadAsStringAsync());
        return null;
    }

    public async Task<bool> DeleteRepo(Guid projectId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/manage/repo/{projectId}", cancellationToken);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
            return true;
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new ProjectSyncInProgressException(projectId);
        logger.LogError("Failed to delete repo: {StatusCode} {StatusDescription}, projectId: {ProjectId}, response: {Response}",
            response.StatusCode,
            response.ReasonPhrase,
            projectId,
            await response.Content.ReadAsStringAsync(cancellationToken));
        return false;
    }
}
