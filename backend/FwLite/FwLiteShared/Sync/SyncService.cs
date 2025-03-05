using System.Diagnostics;
using FwLiteShared.Auth;
using FwLiteShared.Events;
using FwLiteShared.Projects;
using LcmCrdt;
using LcmCrdt.RemoteSync;
using LcmCrdt.Utils;
using Microsoft.EntityFrameworkCore;
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
    ProjectEventBus changeEventBus,
    LexboxProjectService lexboxProjectService,
    IMiniLcmApi lexboxApi,
    ILogger<SyncService> logger,
    LcmCrdtDbContext dbContext)
{
    public async Task<SyncResults> SafeExecuteSync(bool skipNotifications = false)
    {
        try
        {
            return await ExecuteSync(skipNotifications);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to sync project");
            return new SyncResults([], [], false);
        }
    }

    public async Task<SyncResults> ExecuteSync(bool skipNotifications = false)
    {
        var project = await currentProjectService.GetProjectData();
        if (string.IsNullOrEmpty(project.OriginDomain))
        {
            logger.LogWarning("Project {ProjectName} has no origin domain, unable to create http sync client",
                project.Name);
            return new SyncResults([], [], false);
        }

        var oAuthClient = oAuthClientFactory.GetClient(project);
        var httpClient = await oAuthClient.CreateHttpClient();
        if (httpClient is null)
        {
            logger.LogWarning(
                "Unable to create http client to sync project {ProjectName}, user is not authenticated to {OriginDomain}",
                project.Name,
                project.OriginDomain);
            return new SyncResults([], [], false);
        }
        var currentUser = await oAuthClient.GetCurrentUser();
        await currentProjectService.UpdateLastUser(currentUser?.Name, currentUser?.Id);

        var remoteModel = await remoteSyncServiceServer.CreateProjectSyncable(project, httpClient);
        var syncDate = DateTimeOffset.UtcNow;//create sync date first to ensure it's consistent and not based on how long it takes to sync
        var syncResults = await dataModel.SyncWith(remoteModel);
        if (!syncResults.IsSynced)
        {
            logger.LogWarning("Did not sync with server, {ProjectName}", project.Name);
            return syncResults;
        }
        await UpdateSyncDate(syncDate);
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
                               .SelectMany(e => GetEntryId(e.DbObject as IObjectWithId))
                               .Distinct())
            {
                if (entryId is null) continue;
                var entry = await lexboxApi.GetEntry(entryId.Value);
                if (entry is not null)
                {
                    changeEventBus.PublishEntryChangedEvent(currentProjectService.Project, entry);
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

    private async IAsyncEnumerable<Guid?> GetEntryId(IObjectWithId? entity)
    {
        switch (entity)
        {
            case Entry entry:
                yield return entry.Id;
                break;
            case Sense sense:
                yield return sense.EntryId;
                break;
            case ExampleSentence exampleSentence:
                yield return (await dataModel.GetLatest<Sense>(exampleSentence.SenseId))?.EntryId;
                break;
            case ComplexFormComponent complexFormComponent:
                yield return complexFormComponent.ComplexFormEntryId;
                yield return complexFormComponent.ComponentEntryId;
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Note this will update any commits, not just the ones that were synced. This includes ours which we just sent
    /// </summary>
    private async Task UpdateSyncDate(DateTimeOffset syncDate)
    {
        try
        {
            //the prop name is hardcoded into the sql so we just want to assert it's what we expect
            Debug.Assert(CommitHelpers.SyncDateProp == "SyncDate");
            await dbContext.Database.ExecuteSqlAsync(
                $"""
                 UPDATE Commits
                 SET metadata = json_set(metadata, '$.ExtraMetadata.SyncDate', {syncDate.ToString("u")})
                 WHERE json_extract(Metadata, '$.ExtraMetadata.SyncDate') IS NULL;
                 """);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update sync date");
        }
    }

    public async Task UploadProject(Guid lexboxProjectId, LexboxServer server)
    {
        await currentProjectService.SetProjectSyncOrigin(server.Authority, lexboxProjectId);
        try
        {
            await ExecuteSync(true);
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
