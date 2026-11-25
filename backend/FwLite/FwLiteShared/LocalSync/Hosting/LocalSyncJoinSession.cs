using System.Text.Json.Serialization;
using LcmCrdt;
using LcmCrdt.LocalSync;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.LocalSync.Hosting;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JoinSessionState
{
    NotJoined,
    /// <summary>Asked to join; waiting for the host to accept.</summary>
    Waiting,
    Joined,
    /// <summary>The host declined, or refused this copy of the project.</summary>
    Rejected,
    /// <summary>The host stopped sharing, or this device left.</summary>
    Ended,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LocalSyncProblem
{
    /// <summary>Couldn't reach the host; it keeps retrying.</summary>
    HostUnreachable,
    /// <summary>Asking to join failed (the host couldn't be reached, or didn't answer in time); nothing retries.</summary>
    CouldNotJoin,
    /// <summary>The host is sharing a different project.</summary>
    DifferentProject,
    /// <summary>The host has a separate copy of this project that can't be merged.</summary>
    DifferentHistory,
    /// <summary>The host's copy shares no writing system with this one.</summary>
    NoWritingSystemInCommon,
    Failed,
}

/// <param name="Rejection">Why the host turned this device away, when <see cref="State"/> is Rejected.</param>
/// <param name="Problem">What went wrong with the last sync, if anything.</param>
public record JoinSessionView(
    JoinSessionState State,
    string? HostAddress,
    string? ProjectName,
    DateTimeOffset? LastSync,
    JoinRejection? Rejection,
    LocalSyncProblem? Problem);

/// <summary>
/// This device's membership of a host's session, for the current project: joining, then syncing on its own every
/// <see cref="SyncInterval"/> (and on request) until it leaves, the session ends, or the project closes.
/// </summary>
public sealed class LocalSyncJoinSession(
    LocalSyncJoiner joiner,
    CurrentAuthorService currentAuthorService,
    IDeviceName deviceName,
    ILocalNetworkAccess networkAccess,
    ILogger<LocalSyncJoinSession> logger) : IAsyncDisposable
{
    public TimeSpan SyncInterval { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>After this many syncs in a row can't reach the host, look for it at a new address.</summary>
    public int MissesBeforeSearching { get; set; } = 2;

    /// <summary>How hosts are found again after a move; mDNS by default, replaceable in tests.</summary>
    public Func<TimeSpan, CancellationToken, Task<IReadOnlyList<DiscoveredHost>>> Browse { get; set; } = LocalSyncDiscovery.Browse;

    private static readonly TimeSpan SearchFor = TimeSpan.FromSeconds(3);

    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private JoinSessionView _view = new(JoinSessionState.NotJoined, null, null, null, null, null);
    private Uri? _host;
    private JoinStatus? _approved;
    private CancellationTokenSource? _session;
    private IDisposable? _networkLease;
    private int _misses;

    public JoinSessionView View
    {
        get { lock (_lock) return _view; }
    }

    /// <summary>Asks the host to join and returns straight away; <see cref="View"/> follows what happens next.</summary>
    /// <param name="address">The host's address as shown on its screen, e.g. <c>192.168.137.1</c> or <c>192.168.137.1:5139</c>.</param>
    public async Task Join(string address)
    {
        await Leave();
        var host = ParseAddress(address) ?? throw new ArgumentException($"'{address}' isn't a host address", nameof(address));
        var session = new CancellationTokenSource();
        lock (_lock)
        {
            _host = host;
            _session = session;
            _networkLease = networkAccess.Acquire();
            _view = new JoinSessionView(JoinSessionState.Waiting, host.Authority, null, null, null, null);
        }
        _ = Task.Run(async () => await RunSession(host, session.Token));
    }

    private async Task RunSession(Uri host, CancellationToken cancellationToken)
    {
        JoinStatus status;
        try
        {
            status = await joiner.Join(host, DisplayName(), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Couldn't join the local sync host at {Host}", host);
            EndAttempt(cancellationToken, v => v with { State = JoinSessionState.Ended, Problem = LocalSyncProblem.CouldNotJoin });
            return;
        }

        if (status.State != JoinState.Approved)
        {
            EndAttempt(cancellationToken, v => v with { State = JoinSessionState.Rejected, Rejection = status.Rejection });
            return;
        }
        bool left;
        lock (_lock)
        {
            // Leave cancels under this lock, so it can't slip in between
            left = cancellationToken.IsCancellationRequested;
            if (!left)
            {
                _approved = status;
                _view = _view with { State = JoinSessionState.Joined, ProjectName = status.Project?.ProjectName };
            }
        }
        if (left)
        {
            await joiner.Leave(host, status);
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            await SyncNow();
            if (View.State != JoinSessionState.Joined) return;
            try
            {
                await Task.Delay(SyncInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>Syncs with the host now, if joined.</summary>
    public async Task<JoinSessionView> SyncNow()
    {
        Uri? host;
        JoinStatus? approved;
        lock (_lock)
        {
            host = _host;
            approved = _approved;
        }
        if (host is null || approved is null || View.State != JoinSessionState.Joined) return View;

        await _syncLock.WaitAsync();
        try
        {
            var result = await joiner.Sync(host, approved);
            if (result.Problem == JoinerSyncProblem.HostUnreachable && ++_misses >= MissesBeforeSearching
                && await FindMovedHost(host, approved) is { } moved)
            {
                host = moved;
                result = await joiner.Sync(host, approved);
            }
            if (result.Problem != JoinerSyncProblem.HostUnreachable) _misses = 0;

            if (result.Problem == JoinerSyncProblem.SessionEnded)
            {
                logger.LogInformation("The local sync host at {Host} ended the session", host);
                EndSession(JoinSessionState.Ended);
            }
            else
            {
                var problem = ProblemOf(result);
                if (problem != View.Problem && problem is not null)
                    logger.LogWarning(result.Error, "Local sync with {Host}: {Problem}", host, problem);
                Update(v => v with
                {
                    LastSync = result.Synced ? DateTimeOffset.UtcNow : v.LastSync,
                    Problem = problem,
                });
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Local sync with {Host} failed", host);
            Update(v => v with { Problem = LocalSyncProblem.Failed });
        }
        finally
        {
            _syncLock.Release();
        }
        return View;
    }

    /// <summary>
    /// Looks for the host at a new address (its address changed, or this device came back to its network): a host
    /// announcing the same project that still accepts this device's token. Moves the session there if found.
    /// </summary>
    private async Task<Uri?> FindMovedHost(Uri host, JoinStatus approved)
    {
        CancellationToken cancellationToken;
        lock (_lock) cancellationToken = _session?.Token ?? new CancellationToken(canceled: true);
        IReadOnlyList<DiscoveredHost> found;
        try
        {
            using var _ = networkAccess.AcquireForDiscovery();
            found = await Browse(SearchFor, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Couldn't look for the local sync host on the network");
            return null;
        }
        foreach (var candidate in found.Where(h => h.ProjectId == approved.Project?.ProjectId && h.Host != host))
        {
            if (!await joiner.IsApprovedBy(candidate.Host, approved)) continue;
            lock (_lock)
            {
                if (_host != host) return null;
                _host = candidate.Host;
                _view = _view with { HostAddress = candidate.Host.Authority };
            }
            logger.LogInformation("Found the local sync host again at {Host} (was {OldHost})", candidate.Host, host);
            return candidate.Host;
        }
        return null;
    }

    /// <summary>
    /// Stops syncing and tells the host this device is leaving. Stopping comes first and telling the host is best
    /// effort, so leaving never waits on a host that's gone. Safe to call when not joined.
    /// </summary>
    public async Task Leave()
    {
        var (host, approved) = EndSession(state: null);
        if (host is not null && approved is not null) await joiner.Leave(host, approved);
    }

    /// <summary>Tells the hosts this device found that it's nearby with the app open. Best effort.</summary>
    public async Task SayHelloTo(IEnumerable<Uri> hosts)
    {
        var name = DisplayName();
        await Task.WhenAll(hosts.Select(host => joiner.SayHello(host, name)));
    }

    private string DisplayName() => currentAuthorService.GetCurrentAuthor().Name ?? deviceName.Name;

    /// <param name="state">Null for Ended if there was a session, NotJoined if not.</param>
    /// <returns>The host and approval the session had, to tell the host it's left.</returns>
    private (Uri? Host, JoinStatus? Approved) EndSession(JoinSessionState? state)
    {
        lock (_lock)
        {
            var ended = (_host, _approved);
            state ??= _host is null ? JoinSessionState.NotJoined : JoinSessionState.Ended;
            _session?.Cancel();
            _session?.Dispose();
            _session = null;
            _networkLease?.Dispose();
            _networkLease = null;
            _approved = null;
            _host = null;
            _misses = 0;
            _view = _view with { State = state.Value, Problem = null };
            return ended;
        }
    }

    // Releases the network lease unless a Leave or a new Join already took over
    private void EndAttempt(CancellationToken attempt, Func<JoinSessionView, JoinSessionView> change)
    {
        lock (_lock)
        {
            if (_session?.Token != attempt) return;
            _networkLease?.Dispose();
            _networkLease = null;
            _view = change(_view);
        }
    }

    private void Update(Func<JoinSessionView, JoinSessionView> change)
    {
        lock (_lock) _view = change(_view);
    }

    private static LocalSyncProblem? ProblemOf(JoinerSyncResult result)
    {
        if (result.Problem == JoinerSyncProblem.HostUnreachable) return LocalSyncProblem.HostUnreachable;
        if (result.Synced) return null;
        return result.Sync?.Comparison switch
        {
            IdentityComparison.DifferentProject => LocalSyncProblem.DifferentProject,
            IdentityComparison.DifferentRootCommit => LocalSyncProblem.DifferentHistory,
            IdentityComparison.DifferentWritingSystems => LocalSyncProblem.NoWritingSystemInCommon,
            _ => LocalSyncProblem.Failed,
        };
    }

    /// <summary>Accepts what a person reads off the host's screen: an address, with or without the port or http://.</summary>
    public static Uri? ParseAddress(string address)
    {
        address = address.Trim();
        if (!address.Contains("://")) address = "http://" + address;
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp) return null;
        var port = uri.IsDefaultPort ? LocalSyncHosting.DefaultPort : uri.Port;
        return new Uri($"http://{uri.Host}:{port}");
    }

    public async ValueTask DisposeAsync() => await Leave();
}
