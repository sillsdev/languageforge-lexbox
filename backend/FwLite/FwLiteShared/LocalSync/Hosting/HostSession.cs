using System.Security.Cryptography;
using LcmCrdt.LocalSync;

namespace FwLiteShared.LocalSync.Hosting;

/// <param name="VersionMismatch">The joiner runs a different app or protocol version: shown to the host, never refused.</param>
/// <param name="Address">Where its join request came from.</param>
public record JoinerPeer(Guid RequestId, string DisplayName, string AppVersion, bool VersionMismatch, JoinState State, DateTimeOffset? LastSync, string? Address = null);

/// <summary>A device with the app open that said hello but hasn't asked to join.</summary>
public record NearbyDevice(string Address, string DisplayName, string AppVersion, bool VersionMismatch);

/// <summary>
/// The state of one hosting session: who asked to join, who was approved, and their tokens. Nothing here outlives
/// the session. Independent of the HTTP server so it can be reasoned about (and tested) on its own.
/// </summary>
public class HostSession(HostedProject project, string appVersion, TimeProvider? timeProvider = null)
{
    /// <summary>
    /// The join route is open to anyone on the network, so a session keeps at most this many requests. When it's
    /// full, declined requests and joiners not heard from for <see cref="AbandonedAfter"/> make room.
    /// </summary>
    public const int MaxJoinRequests = 50;

    public static readonly TimeSpan AbandonedAfter = TimeSpan.FromMinutes(2);

    /// <summary>The same cap for devices that only said hello.</summary>
    public const int MaxNearbyDevices = 50;

    /// <summary>A device that stops saying hello (it closed the app, or left the network) drops off the list after this.</summary>
    public static readonly TimeSpan NearbyTimeout = TimeSpan.FromSeconds(45);

    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, Joiner> _joiners = [];
    private readonly Dictionary<string, (Hello Hello, DateTimeOffset SeenAt)> _hellos = [];
    private bool _autoApprove;
    private bool _ended;

    private record Joiner(Guid RequestId, JoinRequest Request, bool VersionMismatch, string? Address)
    {
        public DateTimeOffset LastSeen { get; set; }
        public JoinState State { get; set; } = JoinState.Pending;
        public JoinRejection? Rejection { get; set; }
        public string? Token { get; set; }
        public DateTimeOffset? LastSync { get; set; }
    }

    public HostedProject Project => project;

    public bool AutoApprove
    {
        get { lock (_lock) return _autoApprove; }
        set
        {
            lock (_lock)
            {
                _autoApprove = value;
                if (value)
                {
                    foreach (var joiner in _joiners.Values.Where(j => j.State == JoinState.Pending)) ApproveLocked(joiner);
                }
            }
        }
    }

    public IReadOnlyList<JoinerPeer> Peers
    {
        get
        {
            lock (_lock)
            {
                return [.. _joiners.Values
                    .Where(j => j.State != JoinState.Rejected)
                    .Select(j => new JoinerPeer(j.RequestId, j.Request.DisplayName, j.Request.AppVersion, j.VersionMismatch, j.State, j.LastSync, j.Address))];
            }
        }
    }

    /// <param name="current">The hosted project as it is now (its writing systems can change during a session);
    /// the project the session started with by default.</param>
    /// <param name="address">Where the request came from, so the device isn't also listed as nearby.</param>
    /// <exception cref="HostSessionFullException">The session already holds <see cref="MaxJoinRequests"/> requests.</exception>
    public Guid RequestJoin(JoinRequest request, HostedProject? current = null, string? address = null)
    {
        var joiner = new Joiner(Guid.NewGuid(), request,
            VersionMismatch: request.AppVersion != appVersion || request.ProtocolVersion != LocalSyncProtocol.Version,
            address);
        lock (_lock)
        {
            joiner.LastSeen = _time.GetUtcNow();
            if (_joiners.Count >= MaxJoinRequests) MakeRoom(joiner.LastSeen);
            if (_joiners.Count >= MaxJoinRequests) throw new HostSessionFullException();
            _joiners[joiner.RequestId] = joiner;
            if (_ended) Reject(joiner, JoinRejection.SessionEnded);
            else if (HasDifferentHistory(current ?? project, request.Project)) Reject(joiner, JoinRejection.DifferentHistory);
            else if (_autoApprove) ApproveLocked(joiner);
        }
        return joiner.RequestId;
    }

    /// <summary>
    /// The host's half of the identity check: a copy of the same project with a different history is refused before
    /// anyone is asked. A different project, or no copy at all, is the joiner's decision (switch or download).
    /// </summary>
    private static bool HasDifferentHistory(HostedProject hosted, JoinerProject? joinerProject)
    {
        if (joinerProject is null || joinerProject.ProjectId != hosted.ProjectId) return false;
        var comparison = ProjectIdentity.Compare(
            new ProjectIdentity(hosted.ProjectId, hosted.RootCommitId, hosted.WritingSystemIds.ToHashSet()),
            new ProjectIdentity(joinerProject.ProjectId, joinerProject.RootCommitId, joinerProject.WritingSystemIds.ToHashSet()));
        return comparison != IdentityComparison.Match;
    }

    /// <summary>Records that a device with the app open is nearby; it's listed until it stops saying hello.</summary>
    public void SayHello(string address, Hello hello)
    {
        lock (_lock)
        {
            if (_ended) return;
            var now = _time.GetUtcNow();
            if (!_hellos.ContainsKey(address))
            {
                foreach (var gone in _hellos.Where(h => now - h.Value.SeenAt > NearbyTimeout).Select(h => h.Key).ToList())
                    _hellos.Remove(gone);
                if (_hellos.Count >= MaxNearbyDevices) _hellos.Remove(_hellos.MinBy(h => h.Value.SeenAt).Key);
            }
            _hellos[address] = (hello, now);
        }
    }

    /// <summary>Devices that said hello recently and haven't asked to join (those are listed in <see cref="Peers"/>).</summary>
    public IReadOnlyList<NearbyDevice> NearbyDevices
    {
        get
        {
            lock (_lock)
            {
                var now = _time.GetUtcNow();
                var joined = _joiners.Values.Where(j => j.State != JoinState.Rejected).Select(j => j.Address).ToHashSet();
                return [.. _hellos
                    .Where(h => now - h.Value.SeenAt <= NearbyTimeout && !joined.Contains(h.Key))
                    .Select(h => new NearbyDevice(h.Key, h.Value.Hello.DisplayName, h.Value.Hello.AppVersion,
                        h.Value.Hello.AppVersion != appVersion))];
            }
        }
    }

    public JoinStatus? GetJoinStatus(Guid requestId)
    {
        lock (_lock)
        {
            if (!_joiners.TryGetValue(requestId, out var joiner)) return null;
            joiner.LastSeen = _time.GetUtcNow();
            return joiner.State switch
            {
                JoinState.Approved => new JoinStatus(JoinState.Approved, joiner.Token, project, appVersion),
                JoinState.Rejected => new JoinStatus(JoinState.Rejected, Rejection: joiner.Rejection),
                _ => new JoinStatus(JoinState.Pending),
            };
        }
    }

    public void Approve(Guid requestId) => Decide(requestId, joiner => ApproveLocked(joiner));

    public void Decline(Guid requestId) => Decide(requestId, joiner => Reject(joiner, JoinRejection.Declined));

    private void Decide(Guid requestId, Action<Joiner> decision)
    {
        lock (_lock)
        {
            if (_ended || !_joiners.TryGetValue(requestId, out var joiner) || joiner.State != JoinState.Pending) return;
            decision(joiner);
        }
    }

    public bool IsAuthorized(string token)
    {
        lock (_lock)
        {
            if (FindByToken(token) is not { } joiner) return false;
            joiner.LastSeen = _time.GetUtcNow();
            return true;
        }
    }

    /// <summary>Records that an approved joiner just synced, for the hosting panel's peer list.</summary>
    public void MarkSynced(string token)
    {
        lock (_lock)
        {
            var joiner = FindByToken(token);
            if (joiner is null) return;
            joiner.LastSync = _time.GetUtcNow();
        }
    }

    public void Leave(string token)
    {
        lock (_lock)
        {
            var joiner = FindByToken(token);
            if (joiner is null) return;
            _joiners.Remove(joiner.RequestId);
        }
    }

    /// <summary>A joiner that stopped waiting for the host to decide (it left, or gave up).</summary>
    public void Withdraw(Guid requestId)
    {
        lock (_lock)
        {
            if (_joiners.TryGetValue(requestId, out var joiner) && joiner.State == JoinState.Pending)
                _joiners.Remove(requestId);
        }
    }

    private void MakeRoom(DateTimeOffset now)
    {
        foreach (var gone in _joiners.Values.Where(j => j.State == JoinState.Rejected || now - j.LastSeen > AbandonedAfter).ToList())
            _joiners.Remove(gone.RequestId);
    }

    /// <summary>Revokes every token and turns away anyone still waiting. Nothing about the session is kept.</summary>
    public void End()
    {
        lock (_lock)
        {
            _ended = true;
            _hellos.Clear();
            foreach (var joiner in _joiners.Values)
            {
                joiner.Token = null;
                if (joiner.State == JoinState.Pending) Reject(joiner, JoinRejection.SessionEnded);
            }
        }
    }

    private Joiner? FindByToken(string token) =>
        _ended ? null : _joiners.Values.FirstOrDefault(j => j.Token is not null && CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(j.Token), System.Text.Encoding.UTF8.GetBytes(token)));

    private static void ApproveLocked(Joiner joiner)
    {
        joiner.State = JoinState.Approved;
        joiner.Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static void Reject(Joiner joiner, JoinRejection rejection)
    {
        joiner.State = JoinState.Rejected;
        joiner.Rejection = rejection;
    }
}

public class HostSessionFullException() : Exception($"This session already has {HostSession.MaxJoinRequests} join requests");
