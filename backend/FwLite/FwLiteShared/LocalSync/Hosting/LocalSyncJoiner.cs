using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LcmCrdt;
using LcmCrdt.LocalSync;
using LcmCrdt.RemoteSync;
using Microsoft.Extensions.Caching.Memory;
using Refit;

namespace FwLiteShared.LocalSync.Hosting;

public enum JoinerSyncProblem
{
    /// <summary>The host can't be reached (stopped, asleep, or out of range); worth retrying.</summary>
    HostUnreachable,
    /// <summary>The host no longer accepts this device's token (it stopped hosting, or the device left); rejoin.</summary>
    SessionEnded,
}

/// <param name="Sync">Null when the sync didn't get as far as the identity check.</param>
/// <param name="Error">What went wrong reaching the host, for the log.</param>
public record JoinerSyncResult(LocalSyncResult? Sync, JoinerSyncProblem? Problem = null, Exception? Error = null)
{
    public bool Synced => Sync?.Synced == true;
}

/// <summary>
/// The joiner's side of a local sync session, for the current project: ask to join, wait for the host to approve,
/// then sync through the same <see cref="CrdtHttpSyncService"/> used for Lexbox, carrying the session token.
/// </summary>
public class LocalSyncJoiner(
    IHttpClientFactory httpClientFactory,
    CurrentProjectService currentProjectService,
    ProjectIdentityService projectIdentityService,
    CrdtHttpSyncService crdtHttpSyncService,
    LocalSyncService localSyncService,
    IMemoryCache cache)
{
    /// <summary>Its sockets go over the local network (<see cref="ILocalNetworkAccess.Bind"/>), whatever the app's default network is.</summary>
    public const string HttpClientName = "local-sync";

    private static readonly JsonSerializerOptions ProtocolJson = new(JsonSerializerDefaults.Web);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>How long to wait for the host to decide before giving up.</summary>
    public TimeSpan JoinTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How long a small request (join, status, the identity check before a sync) waits for the host. A host that has
    /// gone (left the network, changed address) often never answers at all, and HttpClient's own timeout is 100 s.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Leaving and saying hello are courtesies; the host copes without them, so they don't wait long.</summary>
    public TimeSpan CourtesyTimeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>Asks the host to join and waits until it approves or rejects.</summary>
    /// <exception cref="TimeoutException">The host didn't decide within <see cref="JoinTimeout"/>.</exception>
    public async Task<JoinStatus> Join(Uri host, string displayName, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(JoinTimeout);
        try
        {
            return await JoinUntilDecided(host, displayName, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("The host didn't answer the join request in time");
        }
    }

    private async Task<JoinStatus> JoinUntilDecided(Uri host, string displayName, CancellationToken cancellationToken)
    {
        var identity = await projectIdentityService.GetIdentity();
        var request = new JoinRequest(displayName, AppVersion.Version, LocalSyncProtocol.Version,
            new JoinerProject(identity.ProjectId, identity.RootCommitId, [.. identity.WritingSystemIds]));
        using var client = CreateClient(host, token: null);

        JoinResponse join;
        using (var requestTimeout = TimeoutAfter(RequestTimeout, cancellationToken))
        {
            using var response = await client.PostAsJsonAsync(LocalSyncProtocol.JoinPath, request, ProtocolJson, requestTimeout.Token);
            response.EnsureSuccessStatusCode();
            join = await response.Content.ReadFromJsonAsync<JoinResponse>(ProtocolJson, requestTimeout.Token)
                   ?? throw new InvalidOperationException("The host sent an empty join response");
        }
        try
        {
            while (true)
            {
                using var requestTimeout = TimeoutAfter(RequestTimeout, cancellationToken);
                var status = await client.GetFromJsonAsync<JoinStatus>($"{LocalSyncProtocol.JoinPath}/{join.RequestId}", ProtocolJson, requestTimeout.Token)
                             ?? throw new InvalidOperationException("The host sent an empty join status");
                if (status.State != JoinState.Pending) return status;
                await Task.Delay(PollInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await Withdraw(client, join);
            throw;
        }
    }

    /// <summary>Tells the host to drop a request this device stopped waiting on, so it's no longer listed. Best effort.</summary>
    private async Task Withdraw(HttpClient client, JoinResponse join)
    {
        using var requestTimeout = TimeoutAfter(CourtesyTimeout);
        try
        {
            using var _ = await client.PostAsJsonAsync(LocalSyncProtocol.WithdrawPath, join, ProtocolJson, requestTimeout.Token);
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// Syncs the current project with an approving host. The identity check runs first, against the host's project
    /// as it is now, so a host sharing a different project, or the same one with a different history, is never merged.
    /// Losing the host, or the host ending the session, comes back as a <see cref="JoinerSyncProblem"/>.
    /// </summary>
    public async Task<JoinerSyncResult> Sync(Uri host, JoinStatus approved)
    {
        if (approved is not { State: JoinState.Approved, Token: { } token })
            throw new ArgumentException("Can only sync with a host that approved this device", nameof(approved));

        var client = CreateClient(host, token);
        try
        {
            HostedProject hosted;
            using (var requestTimeout = TimeoutAfter(RequestTimeout))
            {
                hosted = await client.GetFromJsonAsync<HostedProject>(LocalSyncProtocol.ProjectPath, ProtocolJson, requestTimeout.Token)
                         ?? throw new InvalidOperationException("The host sent an empty project");
            }
            // a stopped or briefly unreachable host must not stay marked unhealthy for the half hour the cache keeps it
            CrdtHttpSyncService.InvalidateServerHealth(cache, host.Authority);
            var remote = await crdtHttpSyncService.CreateProjectSyncable(currentProjectService.ProjectData, client);
            var hostIdentity = new ProjectIdentity(hosted.ProjectId, hosted.RootCommitId, hosted.WritingSystemIds.ToHashSet());
            return new JoinerSyncResult(await localSyncService.SyncWith(remote, hostIdentity));
        }
        catch (Exception e) when (IsUnauthorized(e))
        {
            return new JoinerSyncResult(null, JoinerSyncProblem.SessionEnded, e);
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException)
        {
            return new JoinerSyncResult(null, JoinerSyncProblem.HostUnreachable, e);
        }
    }

    /// <summary>
    /// Whether the host at <paramref name="host"/> still accepts this device's token: the same host at a new address,
    /// rather than another device sharing the same project.
    /// </summary>
    public async Task<bool> IsApprovedBy(Uri host, JoinStatus approved)
    {
        if (approved.Token is null) return false;
        using var client = CreateClient(host, approved.Token);
        using var requestTimeout = TimeoutAfter(RequestTimeout);
        try
        {
            using var response = await client.GetAsync(LocalSyncProtocol.ProjectPath, requestTimeout.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>Tells a host this device has the app open nearby, so it's listed before it asks to join. Best effort.</summary>
    public async Task SayHello(Uri host, string displayName)
    {
        using var client = CreateClient(host, token: null);
        using var requestTimeout = TimeoutAfter(CourtesyTimeout);
        try
        {
            using var _ = await client.PostAsJsonAsync(LocalSyncProtocol.HelloPath, new Hello(displayName, AppVersion.Version),
                ProtocolJson, requestTimeout.Token);
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException)
        {
        }
    }

    /// <summary>A 401 anywhere in the chain, including Refit's and the "likely out of date" wrapper Lexbox sync uses.</summary>
    private static bool IsUnauthorized(Exception? e)
    {
        for (; e is not null; e = e.InnerException)
        {
            if (e is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized }
                or ApiException { StatusCode: HttpStatusCode.Unauthorized }) return true;
        }
        return false;
    }

    /// <summary>Tells the host this device has left. Best effort: the host may already be gone.</summary>
    public async Task Leave(Uri host, JoinStatus approved)
    {
        if (approved.Token is null) return;
        using var client = CreateClient(host, approved.Token);
        using var requestTimeout = TimeoutAfter(CourtesyTimeout);
        try
        {
            using var _ = await client.PostAsync(LocalSyncProtocol.LeavePath, content: null, requestTimeout.Token);
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException)
        {
        }
    }

    private static CancellationTokenSource TimeoutAfter(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(timeout);
        return source;
    }

    private HttpClient CreateClient(Uri host, string? token)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        client.BaseAddress = host;
        if (token is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
