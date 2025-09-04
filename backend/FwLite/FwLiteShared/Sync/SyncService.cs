using System.Diagnostics;
using FwLiteShared.Auth;
using FwLiteShared.Events;
using FwLiteShared.Projects;
using LexCore.Sync;
using LcmCrdt;
using LcmCrdt.Data;
using LcmCrdt.MediaServer;
using LcmCrdt.RemoteSync;
using LcmCrdt.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm;
using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Changes;

namespace FwLiteShared.Sync;

public record PendingCommits(int Local, int? Remote);

public class SyncService(
    DataModel dataModel,
    CrdtHttpSyncService remoteSyncServiceServer,
    OAuthClientFactory oAuthClientFactory,
    CurrentProjectService currentProjectService,
    ProjectEventBus changeEventBus,
    LexboxProjectService lexboxProjectService,
    IMiniLcmApi lexboxApi,
    LcmMediaService lcmMediaService,
    IOptions<AuthConfig> authOptions,
    ILogger<SyncService> logger,
    SyncRepository syncRepository)
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
            UpdateSyncStatus(SyncStatus.NoServer);
            return new SyncResults([], [], false);
        }
        if (!authOptions.Value.TryGetServer(project, out var server))
        {
            logger.LogWarning("Unable to find server for project {ProjectName}", project.Name);
            UpdateSyncStatus(SyncStatus.NoServer);
            return new SyncResults([], [], false);
        }
        var oAuthClient = oAuthClientFactory.GetClient(server);
        var httpClient = await oAuthClient.CreateHttpClient();
        if (httpClient is null)
        {
            logger.LogWarning(
                "Unable to create http client to sync project {ProjectName}, user is not authenticated to {OriginDomain}",
                project.Name,
                project.OriginDomain);
            UpdateSyncStatus(SyncStatus.NotLoggedIn);
            return new SyncResults([], [], false);
        }
        var currentUser = await oAuthClient.GetCurrentUser();
        await currentProjectService.UpdateLastUser(currentUser?.Name, currentUser?.Id);

        var remoteModel = await remoteSyncServiceServer.CreateProjectSyncable(project, httpClient);
        if (!await remoteModel.ShouldSync())
        {
            logger.LogInformation("Unable to connect to server when syncing project {ProjectName}", project.Name);
            UpdateSyncStatus(SyncStatus.Offline);
            return new SyncResults([], [], false);
        }

        await UploadPendingMedia();
        var syncDate = DateTimeOffset.UtcNow;//create sync date first to ensure it's consistent and not based on how long it takes to sync
        var syncResults = await dataModel.SyncWith(remoteModel);
        if (!syncResults.IsSynced)
        {
            logger.LogWarning("Did not sync with server, {ProjectName}", project.Name);
            UpdateSyncStatus(SyncStatus.UnknownError);
            return syncResults;
        }
        logger.LogInformation("Synced project {ProjectName} with server", project.Name);
        UpdateSyncStatus(SyncStatus.Success);
        await syncRepository.UpdateSyncDate(syncDate);
        //need to await this, otherwise the database connection will be closed before the notifications are sent
        if (!skipNotifications) await SendNotifications(syncResults);
        return syncResults;
    }

    public async Task<ProjectSyncStatus> GetSyncStatus()
    {
        var project = await currentProjectService.GetProjectData();
        if (!authOptions.Value.TryGetServer(project, out var server)) return ProjectSyncStatus.Unknown(ProjectSyncStatusErrorCode.NotLoggedIn);
        var status = await lexboxProjectService.GetLexboxSyncStatus(server, project.Id);
        return status;
    }

    public async Task<HttpResponseMessage?> TriggerSync()
    {
        var project = await currentProjectService.GetProjectData();
        if (!authOptions.Value.TryGetServer(project, out var server)) return null;
        return await lexboxProjectService.TriggerLexboxSync(server, project.Id);
    }

    public async Task<SyncJobResult> AwaitSyncFinished()
    {
        var project = await currentProjectService.GetProjectData();
        if (!authOptions.Value.TryGetServer(project, out var server))
        {
            return new SyncJobResult(SyncJobStatusEnum.UnableToAuthenticate, "Unable to authenticate with Lexbox");
        }
        return await lexboxProjectService.AwaitLexboxSyncFinished(server, project.Id);
    }

    public async Task<LexboxServer?> GetCurrentServer()
    {
        var project = await currentProjectService.GetProjectData();
        if (!authOptions.Value.TryGetServer(project, out var server)) return null;
        return server;
    }

    public async Task<PendingCommits?> CountPendingCrdtCommits()
    {
        var project = await currentProjectService.GetProjectData();
        var localSyncState = await dataModel.GetSyncState();
        if (!authOptions.Value.TryGetServer(project, out var server)) return null;
        var localChangesPending = syncRepository.CountPendingCommits(); // Not awaited yet
        var remoteChangesPending = lexboxProjectService.CountPendingCrdtCommits(server, project.Id, localSyncState); // Not awaited yet
        await Task.WhenAll(localChangesPending, remoteChangesPending);
        var localChanges = await localChangesPending;
        var remoteChanges = await remoteChangesPending;
        if (localChanges is null) return null;
        return new PendingCommits(localChanges.Value, remoteChanges);
    }

    public async Task UploadPendingMedia()
    {
        try
        {
            await lcmMediaService.UploadPendingResources();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to upload pending media");
        }
    }

    private void UpdateSyncStatus(SyncStatus status)
    {
        changeEventBus.PublishEvent(currentProjectService.Project, new SyncEvent(status));
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

            foreach (var deleteChange in syncResults.MissingFromLocal
                         .SelectMany(c => c.ChangeEntities, (_, change) => change.Change)
                         .OfType<DeleteChange<Entry>>())
            {
                changeEventBus.PublishEvent(currentProjectService.Project, new EntryDeletedEvent(deleteChange.EntityId));
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
