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
    ILocalNetworkAccess networkAccess,
    ILogger<LocalSyncJoinSession> logger) : IAsyncDisposable
{
    public TimeSpan SyncInterval { get; set; } = TimeSpan.FromSeconds(20);

    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private JoinSessionView _view = new(JoinSessionState.NotJoined, null, null, null, null, null);
    private Uri? _host;
    private JoinStatus? _approved;
    private CancellationTokenSource? _session;
    private IDisposable? _networkLease;

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
            var name = currentAuthorService.GetCurrentAuthor().Name ?? Environment.MachineName;
            status = await joiner.Join(host, name, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Couldn't join the local sync host at {Host}", host);
            Update(v => v with { State = JoinSessionState.Ended, Problem = LocalSyncProblem.HostUnreachable });
            return;
        }

        if (status.State != JoinState.Approved)
        {
            Update(v => v with { State = JoinSessionState.Rejected, Rejection = status.Rejection });
            return;
        }
        lock (_lock) _approved = status;
        Update(v => v with { State = JoinSessionState.Joined, ProjectName = status.Project?.ProjectName });

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
            if (result.Problem == JoinerSyncProblem.SessionEnded)
            {
                await EndSession(JoinSessionState.Ended, problem: null);
            }
            else
            {
                Update(v => v with
                {
                    LastSync = result.Synced ? DateTimeOffset.UtcNow : v.LastSync,
                    Problem = ProblemOf(result),
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

    /// <summary>Tells the host this device is leaving and stops syncing. Safe to call when not joined.</summary>
    public async Task Leave()
    {
        Uri? host;
        JoinStatus? approved;
        lock (_lock)
        {
            host = _host;
            approved = _approved;
        }
        if (host is not null && approved is not null) await joiner.Leave(host, approved);
        await EndSession(_host is null ? JoinSessionState.NotJoined : JoinSessionState.Ended, problem: null);
    }

    private Task EndSession(JoinSessionState state, LocalSyncProblem? problem)
    {
        lock (_lock)
        {
            _session?.Cancel();
            _session?.Dispose();
            _session = null;
            _networkLease?.Dispose();
            _networkLease = null;
            _approved = null;
            _host = null;
            _view = _view with { State = state, Problem = problem };
        }
        return Task.CompletedTask;
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
