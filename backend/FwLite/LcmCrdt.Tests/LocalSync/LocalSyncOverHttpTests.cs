using System.Net;
using FwLiteShared.KeepAwake;
using FwLiteShared.LocalSync;
using FwLiteShared.LocalSync.Hosting;
using static LcmCrdt.Tests.LocalSync.LocalSyncTestHelpers;

namespace LcmCrdt.Tests.LocalSync;

/// <summary>
/// A host and a joiner talking over real HTTP on loopback.
/// </summary>
public class LocalSyncOverHttpTests : IAsyncLifetime
{
    private static readonly Guid ProjectId = Guid.Parse("4f6b8d0a-2c3e-4a5f-9b1d-7e9a1c3e5b80");
    private readonly MiniLcmApiFixture _host = CreateFixture(ProjectId);
    private readonly MiniLcmApiFixture _joiner = CreateFixture(ProjectId, seedWs: false);
    private HostingInfo _hosting = null!;
    private Uri HostAddress => new($"http://127.0.0.1:{_hosting.Port}");

    public async Task InitializeAsync()
    {
        await _host.InitializeAsync();
        await _joiner.InitializeAsync();
        await MakeEditor(_host, "host-user");
        await MakeEditor(_joiner, "joiner-user");
        // the joiner starts as a copy of the host's project
        await _joiner.DataModel.SyncWith(_host.DataModel);
        _hosting = await _host.GetService<LocalSyncHosting>()
            .StartHosting(_host.GetService<CurrentProjectService>().Project, new HostingNetwork.Existing(IPAddress.Loopback));
    }

    public async Task DisposeAsync()
    {
        await _host.GetService<LocalSyncHosting>().StopHosting();
        await _joiner.DisposeAsync();
        await _host.DisposeAsync();
    }

    private LocalSyncJoiner Joiner => JoinerOf(_joiner);

    private static LocalSyncJoiner JoinerOf(MiniLcmApiFixture device)
    {
        var joiner = device.GetService<LocalSyncJoiner>();
        joiner.PollInterval = TimeSpan.FromMilliseconds(20);
        return joiner;
    }

    /// <summary>Joins, deciding on the host's side as soon as the request shows up, like a host user tapping Accept.</summary>
    private async Task<JoinStatus> JoinAndDecide(bool approve = true, MiniLcmApiFixture? device = null)
    {
        var join = JoinerOf(device ?? _joiner).Join(HostAddress, "Amina's phone");
        var session = _hosting.Session;
        JoinerPeer? pending;
        while ((pending = session.Peers.FirstOrDefault(p => p.State == JoinState.Pending)) is null)
        {
            if (join.IsCompleted) return await join;
            await Task.Delay(20);
        }
        if (approve) session.Approve(pending.RequestId);
        else session.Decline(pending.RequestId);
        return await join;
    }

    private Task<JoinStatus> JoinAndApprove() => JoinAndDecide();

    [Fact]
    public async Task AnApprovedJoiner_SyncsWithTheHostOverHttp()
    {
        var onHost = await _host.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "apple" } });
        var onJoiner = await _joiner.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "banana" } });

        var approved = await JoinAndApprove();
        var result = await Joiner.Sync(HostAddress, approved);

        approved.State.Should().Be(JoinState.Approved);
        result.Synced.Should().BeTrue();
        (await _joiner.Api.GetEntry(onHost.Id)).Should().NotBeNull();
        (await _host.Api.GetEntry(onJoiner.Id)).Should().NotBeNull();
        _hosting.Session.Peers.Single().LastSync.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncRequestsWithoutAToken_AreRefused()
    {
        using var client = new HttpClient { BaseAddress = HostAddress };

        var response = await client.GetAsync($"/api/crdt/{ProjectId}/get");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("changes")]
    [InlineData("add")]
    public async Task WritesAndChangeRequestsWithoutAToken_AreRefused(string route)
    {
        using var client = new HttpClient { BaseAddress = HostAddress };

        HttpStatusCode? status;
        try
        {
            using var response = await client.PostAsync($"/api/crdt/{ProjectId}/{route}",
                new StringContent("[]", System.Text.Encoding.UTF8, "application/json"));
            status = response.StatusCode;
        }
        catch (HttpRequestException)
        {
            // Watson resets the connection instead of sending the 401 when the handler leaves the body unread
            status = null;
        }

        (status is null or HttpStatusCode.Unauthorized).Should().BeTrue($"the host answered {status}");
    }

    [Fact]
    public async Task TheHostedProjectWithoutAToken_IsRefused()
    {
        using var client = new HttpClient { BaseAddress = HostAddress };

        var response = await client.GetAsync(LocalSyncProtocol.ProjectPath);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SyncRequestsWithAMadeUpToken_AreRefused()
    {
        using var client = new HttpClient { BaseAddress = HostAddress };
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "made-up");

        var response = await client.GetAsync($"/api/crdt/{ProjectId}/get");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AfterTheHostStops_TheJoinersSyncFailsCleanly()
    {
        var approved = await JoinAndApprove();
        await _host.GetService<LocalSyncHosting>().StopHosting();

        var result = await Joiner.Sync(HostAddress, approved);

        result.Synced.Should().BeFalse();
        result.Problem.Should().Be(JoinerSyncProblem.HostUnreachable);
    }

    [Fact]
    public async Task ADeclinedJoiner_IsToldSo()
    {
        var status = await JoinAndDecide(approve: false);

        status.State.Should().Be(JoinState.Rejected);
        status.Rejection.Should().Be(JoinRejection.Declined);
    }

    [Fact]
    public async Task AJoinerWhoseCopyHasADifferentHistory_IsRefused()
    {
        // same project Id, created independently
        await using var impostor = CreateFixture(ProjectId);
        await impostor.InitializeAsync();

        var status = await JoinAndDecide(device: impostor);

        status.State.Should().Be(JoinState.Rejected);
        status.Rejection.Should().Be(JoinRejection.DifferentHistory);
    }

    [Fact]
    public async Task AfterLeaving_TheJoinerCanNoLongerSync()
    {
        var approved = await JoinAndApprove();
        await Joiner.Leave(HostAddress, approved);

        var result = await Joiner.Sync(HostAddress, approved);

        result.Problem.Should().Be(JoinerSyncProblem.SessionEnded);
        _hosting.Session.Peers.Should().BeEmpty();
    }

    [Fact]
    public async Task AJoinerFromAnEarlierSession_IsToldTheSessionEnded()
    {
        var approved = await JoinAndApprove();
        var hosting = _host.GetService<LocalSyncHosting>();
        await hosting.StopHosting();
        _hosting = await hosting.StartHosting(_host.GetService<CurrentProjectService>().Project, new HostingNetwork.Existing(IPAddress.Loopback));

        var result = await Joiner.Sync(HostAddress, approved);

        result.Problem.Should().Be(JoinerSyncProblem.SessionEnded);
    }

    [Fact]
    public async Task AWritingSystemAddedDuringTheSession_ReachesTheJoiner()
    {
        var approved = await JoinAndApprove();
        await Joiner.Sync(HostAddress, approved);
        // the host adds a writing system after the joiner joined and synced
        await _host.Api.CreateWritingSystem(new WritingSystem
        {
            Id = Guid.NewGuid(), WsId = "pt", Name = "Portuguese", Abbreviation = "pt", Font = "Arial",
            Type = WritingSystemType.Analysis
        });

        var first = await Joiner.Sync(HostAddress, approved);
        var second = await Joiner.Sync(HostAddress, approved);

        first.Synced.Should().BeTrue();
        second.Synced.Should().BeTrue();
        (await _joiner.Api.GetWritingSystems()).Analysis.Select(ws => ws.WsId.Code).Should().Contain("pt");
    }

    [Fact]
    public async Task TheHostStaysAwakeOnlyWhileHosting()
    {
        var platform = HostingKeepAwake(_host);
        platform.Awake.Should().BeTrue();

        await _host.GetService<LocalSyncHosting>().StopHosting();

        platform.Awake.Should().BeFalse();
    }
}

public class RecordingKeepAwakePlatform : IKeepAwakePlatform
{
    public bool Awake { get; private set; }
    public void Acquire(KeepAwakeWork work) => Awake = true;
    public void Release() => Awake = false;
}
