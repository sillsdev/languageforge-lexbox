using FwLiteShared.Auth;
using FwLiteShared.Events;
using FwLiteShared.Projects;
using FwLiteShared.Services;
using LexCore.Sync;
using LcmCrdt;
using LcmCrdt.Data;
using LcmCrdt.MediaServer;
using LcmCrdt.RemoteSync;
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

        // CreateHttpClient validates/refreshes auth-state, so it should be called first
        var httpClient = await oAuthClient.CreateHttpClient();

        // IsSignedIn is more specific than GetAuth (which also returns null if refreshing fails transiently e.g. due to connectivity)
        // Note we prioritize NotLoggedIn over Offline, because it's more actionable for the user/UI
        if (!await oAuthClient.IsSignedIn())
        {
            logger.LogWarning("Unable to sync project {ProjectName}. User is not signed in.", project.Name);
            UpdateSyncStatus(SyncStatus.NotLoggedIn);
            return new SyncResults([], [], false);
        }

        if (httpClient is null)
        {
            // We know there's a token, but apparently it's unusable, which means it couldn't be refreshed => assume offline
            logger.LogWarning("Unable to sync project {ProjectName}. Could not obtain token/http-client.", project.Name);
            UpdateSyncStatus(SyncStatus.Offline);
            return new SyncResults([], [], false);
        }

        try
        {
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
            // Best-effort: if the push listener failed to start at project-open (e.g. user was offline), this
            // restarts it now that we know auth + network are healthy. If it's already running, the cache
            // short-circuits and this is effectively a no-op.
            _ = TryEnsureProjectChangeListener(project);
            //need to await this, otherwise the database connection will be closed before the notifications are sent
            if (!skipNotifications) await SendNotifications(syncResults);
            return syncResults;
        }
        // Connectivity dropped mid-sync, or the device reports online but the server is unreachable (captive
        // portal, or a stale-healthy server-health cache). HttpClient signals a connection failure as
        // HttpRequestException and a request timeout as a (Task)OperationCanceledException; ExecuteSync takes
        // no cancellation token, so a cancellation here is a timeout, not a caller abort. Read either as
        // Offline rather than surfacing a raw connection/timeout error to the caller.
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException)
        {
            logger.LogInformation(e, "Lost connection while syncing project {ProjectName}", project.Name);
            UpdateSyncStatus(SyncStatus.Offline);
            return new SyncResults([], [], false);
        }
    }

    private async Task TryEnsureProjectChangeListener(ProjectData project, CancellationToken cancellationToken = default)
    {
        try
        {
            // The sync just proved this server reachable, which licenses kicking a mid-backoff reconnect.
            await lexboxProjectService.ListenForProjectChanges(project, cancellationToken, kickReconnecting: true);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to (re)start project change listener after sync");
        }
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
            var deletedEntryIds = syncResults.MissingFromLocal
                .SelectMany(c => c.ChangeEntities, (_, change) => change.Change)
                .OfType<DeleteChange<Entry>>()
                .Select(c => c.EntityId)
                .ToHashSet();

            var changedEntryIds = new List<Guid>();
            await foreach (var entryId in syncResults.MissingFromLocal
                               .SelectMany(c => c.Snapshots, (commit, snapshot) => snapshot.Entity)
                               .ToAsyncEnumerable()
                               .SelectMany(e => GetEntryId(e.DbObject as IObjectWithId))
                               .Distinct())
            {
                if (entryId is null || deletedEntryIds.Contains(entryId.Value)) continue;
                changedEntryIds.Add(entryId.Value);
            }

            if (changedEntryIds.Count == 0 && deletedEntryIds.Count == 0) return;
            changeEventBus.PublishEntriesChanged(currentProjectService.Project, [.. changedEntryIds], [.. deletedEntryIds]);
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
            // ExecuteSync reports an offline/connectivity failure as an unsynced result rather than throwing,
            // so an unsynced result here means the initial upload never reached the server — fall through to
            // the rollback below instead of leaving the project bound to a server it never uploaded to.
            var result = await ExecuteSync(true);
            if (!result.IsSynced)
                throw new InvalidOperationException("Initial project upload did not sync with the server");
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
