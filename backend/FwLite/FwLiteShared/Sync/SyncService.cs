﻿using FwLiteShared.Auth;
using FwLiteShared.Projects;
using LcmCrdt;
using LcmCrdt.RemoteSync;
using Microsoft.Extensions.Logging;
using MiniLcm;
using MiniLcm.Models;
using SIL.Harmony;

namespace FwLiteShared.Sync;

public class SyncService(
    DataModel dataModel,
    CrdtHttpSyncService remoteSyncServiceServer,
    OAuthClientFactory oAuthClientFactory,
    CurrentProjectService currentProjectService,
    ChangeEventBus changeEventBus,
    LexboxProjectService lexboxProjectService,
    IMiniLcmApi lexboxApi,
    ILogger<SyncService> logger)
{
    public async Task<SyncResults> ExecuteSync(bool skipNotifications = false)
    {
        var project = await currentProjectService.GetProjectData();
        if (string.IsNullOrEmpty(project.OriginDomain))
        {
            logger.LogWarning("Project {ProjectName} has no origin domain, unable to create http sync client",
                project.Name);
            return new SyncResults([], [], false);
        }

        var httpClient = await oAuthClientFactory.GetClient(project).CreateHttpClient();
        if (httpClient is null)
        {
            logger.LogWarning(
                "Unable to create http client to sync project {ProjectName}, user is not authenticated to {OriginDomain}",
                project.Name,
                project.OriginDomain);
            return new SyncResults([], [], false);
        }

        var remoteModel = await remoteSyncServiceServer.CreateProjectSyncable(project, httpClient);
        var syncResults = await dataModel.SyncWith(remoteModel);
        //need to await this, otherwise the database connection will be closed before the notifications are sent
        if (!skipNotifications) await SendNotifications(syncResults);
        return syncResults;
    }

    private async Task SendNotifications(SyncResults syncResults)
    {
        try
        {
            await foreach (var entryId in syncResults.MissingFromLocal
                               .SelectMany(c => c.Snapshots, (commit, snapshot) => snapshot.Entity)
                               .ToAsyncEnumerable()
                               .SelectAwait(async e => await GetEntryId(e.DbObject as IObjectWithId))
                               .Distinct())
            {
                if (entryId is null) continue;
                var entry = await lexboxApi.GetEntry(entryId.Value);
                if (entry is not null)
                {
                    changeEventBus.NotifyEntryUpdated(entry, currentProjectService.Project);
                }
                else
                {
                    logger.LogError("Failed to get entry {EntryId}, was not found", entryId);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to send notifications, continuing");
        }
    }

    private async ValueTask<Guid?> GetEntryId(IObjectWithId? entity)
    {
        return entity switch
        {
            Entry entry => entry.Id,
            Sense sense => sense.EntryId,
            ExampleSentence exampleSentence => (await dataModel.GetLatest<Sense>(exampleSentence.SenseId))?.EntryId,
            _ => null
        };
    }

    public async Task UploadProject(Guid lexboxProjectId, LexboxServer server)
    {
        await currentProjectService.SetProjectSyncOrigin(server.Authority, lexboxProjectId);
        try
        {
            await ExecuteSync();
        }
        catch
        {
            await currentProjectService.SetProjectSyncOrigin(null, null);
            throw;
        }

        //todo maybe decouple this
        lexboxProjectService.InvalidateProjectsCache(server);
    }
}
