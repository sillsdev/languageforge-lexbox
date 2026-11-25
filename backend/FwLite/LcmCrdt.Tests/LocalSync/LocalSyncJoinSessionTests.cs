using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using FwLiteShared.LocalSync.Hosting;
using static LcmCrdt.Tests.LocalSync.LocalSyncTestHelpers;

namespace LcmCrdt.Tests.LocalSync;

/// <summary>Counts leases, standing in for Android's Wi-Fi routing and multicast lock.</summary>
public class RecordingLocalNetworkAccess : RefCountedLocalNetworkAccess
{
    public bool Active { get; private set; }
    protected override void Start() => Active = true;
    protected override void Stop() => Active = false;
}

public class LocalSyncJoinSessionTests : IAsyncLifetime
{
    private static readonly Guid ProjectId = Guid.Parse("9a1c3e5f-7b2d-4c6e-8f0a-2b4d6f8a0c13");
    private readonly MiniLcmApiFixture _host = CreateFixture(ProjectId);
    private readonly MiniLcmApiFixture _joiner = CreateFixture(ProjectId, seedWs: false);
    private HostingInfo _hosting = null!;
    private string HostAddress => $"127.0.0.1:{_hosting.Port}";

    public async Task InitializeAsync()
    {
        await _host.InitializeAsync();
        await _joiner.InitializeAsync();
        await MakeEditor(_host, "host-user");
        await MakeEditor(_joiner, "joiner-user");
        await _joiner.DataModel.SyncWith(_host.DataModel);
        _hosting = await _host.GetService<LocalSyncHosting>()
            .StartHosting(_host.GetService<CurrentProjectService>().Project, new HostingNetwork.Existing(IPAddress.Loopback));
        Session.SyncInterval = TimeSpan.FromHours(1);
        _joiner.GetService<LocalSyncJoiner>().PollInterval = TimeSpan.FromMilliseconds(20);
    }

    public async Task DisposeAsync()
    {
        await Session.Leave();
        await _host.GetService<LocalSyncHosting>().StopHosting();
        await _joiner.DisposeAsync();
        await _host.DisposeAsync();
    }

    private LocalSyncJoinSession Session => _joiner.GetService<LocalSyncJoinSession>();
    private RecordingLocalNetworkAccess Network => (RecordingLocalNetworkAccess)_joiner.GetService<ILocalNetworkAccess>();

    private static async Task<T> Eventually<T>(Func<T?> get, string what) where T : class
    {
        for (var i = 0; i < 250; i++)
        {
            if (get() is { } value) return value;
            await Task.Delay(20);
        }
        throw new TimeoutException($"Timed out waiting for {what}");
    }

    private async Task ApproveTheJoiner()
    {
        var pending = await Eventually(() => _hosting.Session.Peers.FirstOrDefault(p => p.State == JoinState.Pending), "the join request");
        _hosting.Session.Approve(pending.RequestId);
    }

    private async Task<JoinSessionView> WaitFor(JoinSessionState state) =>
        await Eventually(() => Session.View.State == state && (state != JoinSessionState.Joined || Session.View.LastSync is not null) ? Session.View : null,
            $"the session to be {state}");

    [Fact]
    public async Task OnceAccepted_TheJoinerSyncsWithTheHost()
    {
        var onHost = await _host.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "apple" } });

        await Session.Join(HostAddress);
        Session.View.State.Should().Be(JoinSessionState.Waiting);
        Network.Active.Should().BeTrue("the device must route over the host's network while joined");
        await ApproveTheJoiner();
        var joined = await WaitFor(JoinSessionState.Joined);

        joined.ProjectName.Should().Be("Sena 3");
        (await _joiner.Api.GetEntry(onHost.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task SyncNow_SendsTheJoinersEditsToTheHost()
    {
        await Session.Join(HostAddress);
        await ApproveTheJoiner();
        await WaitFor(JoinSessionState.Joined);
        var onJoiner = await _joiner.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "banana" } });

        var view = await Session.SyncNow();

        view.Problem.Should().BeNull();
        (await _host.Api.GetEntry(onJoiner.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task ADeclinedJoiner_IsToldSoAndStopsUsingTheNetwork()
    {
        await Session.Join(HostAddress);
        var pending = await Eventually(() => _hosting.Session.Peers.FirstOrDefault(p => p.State == JoinState.Pending), "the join request");
        _hosting.Session.Decline(pending.RequestId);

        var rejected = await WaitFor(JoinSessionState.Rejected);

        rejected.Rejection.Should().Be(JoinRejection.Declined);
        Network.Active.Should().BeFalse();
    }

    [Fact]
    public async Task Leaving_TellsTheHostAndReleasesTheNetwork()
    {
        await Session.Join(HostAddress);
        await ApproveTheJoiner();
        await WaitFor(JoinSessionState.Joined);

        await Session.Leave();

        Session.View.State.Should().Be(JoinSessionState.Ended);
        _hosting.Session.Peers.Should().BeEmpty();
        Network.Active.Should().BeFalse();
    }

    [Fact]
    public async Task WhenTheHostStops_TheSessionEnds()
    {
        await Session.Join(HostAddress);
        await ApproveTheJoiner();
        await WaitFor(JoinSessionState.Joined);
        await _host.GetService<LocalSyncHosting>().StopHosting();

        var view = await Session.SyncNow();

        view.State.Should().Be(JoinSessionState.Joined, "a stopped host looks the same as one that's briefly out of range");
        view.Problem.Should().Be(LocalSyncProblem.HostUnreachable);
    }

    [Fact]
    public async Task WhenTheHostMovesToANewAddress_TheJoinerFindsItAgain()
    {
        await Session.Join(HostAddress);
        await ApproveTheJoiner();
        await WaitFor(JoinSessionState.Joined);
        var hosting = _host.GetService<LocalSyncHosting>();
        await hosting.MoveTo(IPAddress.Parse("127.0.0.2"));
        var moved = hosting.Current!.Invitation.Host;
        Session.MissesBeforeSearching = 1;
        Session.Browse = (_, _) => Task.FromResult<IReadOnlyList<DiscoveredHost>>(
            [new DiscoveredHost(moved, ProjectId, "Sena 3", AppVersion.Version, LocalSyncProtocol.Version)]);
        var onHost = await _host.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "cherry" } });

        var view = await Session.SyncNow();

        view.Problem.Should().BeNull();
        view.HostAddress.Should().Be(moved.Authority);
        (await _joiner.Api.GetEntry(onHost.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task AHostThatNeverAnswers_DoesntHoldUpSyncingOrLeaving()
    {
        // accepts connections (the OS completes them) but never reads or answers a request
        using var silentHost = new TcpListener(IPAddress.Loopback, 0);
        silentHost.Start();
        var host = new Uri($"http://127.0.0.1:{((IPEndPoint)silentHost.LocalEndpoint).Port}");
        var joiner = _joiner.GetService<LocalSyncJoiner>();
        joiner.RequestTimeout = joiner.CourtesyTimeout = TimeSpan.FromMilliseconds(300);
        var approved = new JoinStatus(JoinState.Approved, "token");
        var elapsed = Stopwatch.StartNew();

        var result = await joiner.Sync(host, approved);
        await joiner.Leave(host, approved);

        result.Problem.Should().Be(JoinerSyncProblem.HostUnreachable);
        elapsed.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("192.168.137.1", "http://192.168.137.1:5139/")]
    [InlineData("192.168.137.1:6000", "http://192.168.137.1:6000/")]
    [InlineData(" http://10.42.0.1:5139 ", "http://10.42.0.1:5139/")]
    public void AnAddressReadOffTheHostsScreen_IsUnderstood(string typed, string expected)
    {
        LocalSyncJoinSession.ParseAddress(typed).Should().Be(new Uri(expected));
    }
}
