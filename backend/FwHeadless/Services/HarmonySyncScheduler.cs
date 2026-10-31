using System.Collections.Concurrent;
using LcmCrdt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FwHeadless.Services;

/// <summary>
/// Keeps each headless harmony project in step with Lexbox's CRDT commits, using only a harmony sync
/// (<see cref="CrdtSyncService.SyncHarmonyProject"/>), never the chorus harmony merge. A sync runs after a trailing
/// debounce once a lexicon API write lands or Lexbox reports another client's commits, and a sweep syncs every
/// project once a day, spread over the day. While a merge is queued or running for a project its syncs are dropped,
/// the merge syncs on its own; a project that dropped one is synced again on the first scan after the merge.
/// Like the merge, it never syncs a project that is blocked from syncing or whose first merge hasn't finished.
/// </summary>
public class HarmonySyncScheduler(
    IServiceProvider services,
    SyncHostedService mergeService,
    IOptions<FwHeadlessConfig> config,
    IHttpMessageHandlerFactory httpMessageHandlerFactory,
    IMemoryCache cache,
    ILoggerFactory loggerFactory,
    ILogger<HarmonySyncScheduler> logger) : BackgroundService
{
    internal static readonly TimeSpan Debounce = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);
    internal static readonly TimeSpan SweepInterval = TimeSpan.FromHours(24);
    // how often new projects on disk are picked up for change notifications and the sweep
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<Guid, ProjectSyncState> _projects = new();
    private readonly CancellationTokenSource _shutdown = new();

    private sealed class ProjectSyncState(DateTimeOffset nextSweep)
    {
        public readonly Lock Lock = new();
        public readonly SemaphoreSlim SyncLock = new(1, 1);
        public CancellationTokenSource? Pending;
        public int ConsecutiveFailures;
        public DateTimeOffset NextSweep = nextSweep;
        // the headless harmony project's own client id, learnt on its first sync, so its own pushes don't echo back
        public Guid? ClientId;
        // a sync was dropped for a merge; the merge's own sync may predate the change that asked for it
        public bool DroppedDuringMerge;
        // only touched while holding SyncLock
        public DateTimeOffset LastSyncStarted;
    }

    private ProjectSyncState State(Guid projectId) => _projects.GetOrAdd(projectId,
        _ => new ProjectSyncState(DateTimeOffset.UtcNow + SweepInterval * Random.Shared.NextDouble()));

    /// <summary>
    /// Schedules a harmony sync of the project after the debounce (or the failure backoff), replacing one already pending.
    /// </summary>
    public void RequestSync(Guid projectId)
    {
        if (_shutdown.IsCancellationRequested) return;
        var state = State(projectId);
        if (mergeService.IsJobQueuedOrRunning(projectId))
        {
            logger.LogInformation("Not scheduling a harmony sync of project {ProjectId}, a merge is queued or running", projectId);
            state.DroppedDuringMerge = true;
            return;
        }

        CancellationTokenSource pending;
        TimeSpan delay;
        lock (state.Lock)
        {
            state.Pending?.Cancel();
            pending = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
            state.Pending = pending;
            delay = Delay(state.ConsecutiveFailures);
        }
        _ = SyncAfterDelay(projectId, state, pending, delay);
    }

    internal static TimeSpan Delay(int consecutiveFailures)
    {
        if (consecutiveFailures == 0) return Debounce;
        var backoff = Debounce * Math.Pow(2, Math.Min(consecutiveFailures, 16));
        return backoff < MaxBackoff ? backoff : MaxBackoff;
    }

    private void OnProjectUpdated(Guid projectId, Guid? clientId)
    {
        if (clientId is not null && _projects.TryGetValue(projectId, out var state) && state.ClientId == clientId) return;
        RequestSync(projectId);
    }

    private async Task SyncAfterDelay(Guid projectId, ProjectSyncState state, CancellationTokenSource pending, TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay, pending.Token);
            lock (state.Lock)
            {
                if (state.Pending != pending) return;
                state.Pending = null;
            }
            await SyncNow(projectId, state, requestedAt: DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (pending.IsCancellationRequested)
        {
            // replaced by a later request, or shutting down
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unexpected error scheduling a harmony sync of project {ProjectId}", projectId);
        }
        finally
        {
            pending.Dispose();
        }
    }

    private async Task SyncNow(Guid projectId, ProjectSyncState state, DateTimeOffset requestedAt)
    {
        var retry = false;
        await state.SyncLock.WaitAsync(_shutdown.Token);
        try
        {
            // requests that waited out a running sync need only one more
            if (state.LastSyncStarted > requestedAt) return;
            if (mergeService.IsJobQueuedOrRunning(projectId))
            {
                logger.LogInformation("Dropping harmony sync of project {ProjectId}, a merge is queued or running", projectId);
                state.DroppedDuringMerge = true;
                return;
            }

            await using var scope = services.CreateAsyncScope();
            if (await scope.ServiceProvider.GetRequiredService<IProjectMetadataService>().GetSyncBlockedInfoAsync(projectId) is { IsBlocked: true })
            {
                logger.LogInformation("Skipping harmony sync of project {ProjectId}, it is blocked from syncing", projectId);
                return;
            }
            var project = await scope.ServiceProvider.GetRequiredService<ProjectContextFromIdService>().FindReadyCrdtProject(projectId);
            if (project is null)
            {
                logger.LogWarning("Skipping harmony sync of project {ProjectId}, it has no headless harmony project or its first merge hasn't finished", projectId);
                return;
            }
            state.LastSyncStarted = DateTimeOffset.UtcNow;
            var projectData = await scope.ServiceProvider.GetRequiredService<CurrentProjectService>().SetupProjectContext(project);
            state.ClientId = projectData.ClientId;
            await scope.ServiceProvider.GetRequiredService<CrdtSyncService>().SyncHarmonyProject();
            lock (state.Lock)
            {
                state.ConsecutiveFailures = 0;
                state.NextSweep = DateTimeOffset.UtcNow + SweepInterval;
            }
        }
        catch (Exception e) when (!_shutdown.IsCancellationRequested)
        {
            int failures;
            lock (state.Lock) failures = ++state.ConsecutiveFailures;
            logger.LogError(e, "Harmony sync of project {ProjectId} failed ({Failures} in a row), retrying in {Delay}",
                projectId, failures, Delay(failures));
            retry = true;
        }
        finally
        {
            state.SyncLock.Release();
        }
        if (retry) RequestSync(projectId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var listener = new LexboxProjectChangeListener(httpMessageHandlerFactory,
            config,
            cache,
            loggerFactory,
            loggerFactory.CreateLogger<LexboxProjectChangeListener>(),
            OnProjectUpdated);
        using var timer = new PeriodicTimer(ScanInterval);
        try
        {
            do
            {
                try
                {
                    await Scan(listener, stoppingToken);
                }
                catch (Exception e) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(e, "Failed to scan headless harmony projects");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task Scan(LexboxProjectChangeListener listener, CancellationToken stoppingToken)
    {
        var projectIds = config.Value.HeadlessHarmonyProjectIds().ToArray();
        var now = DateTimeOffset.UtcNow;
        foreach (var projectId in projectIds)
        {
            var state = State(projectId);
            bool due;
            lock (state.Lock)
            {
                due = now >= state.NextSweep;
                if (due) state.NextSweep = now + SweepInterval;
                if (state.DroppedDuringMerge && !mergeService.IsJobQueuedOrRunning(projectId))
                {
                    state.DroppedDuringMerge = false;
                    due = true;
                }
            }
            if (due) RequestSync(projectId);
        }
        await listener.ListenTo(projectIds, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _shutdown.CancelAsync();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _shutdown.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
