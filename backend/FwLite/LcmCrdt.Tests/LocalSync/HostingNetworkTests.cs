using System.Net;
using System.Net.Sockets;
using FwLiteShared.KeepAwake;
using FwLiteShared.LocalSync.Hosting;
using Microsoft.Extensions.DependencyInjection;
using static LcmCrdt.Tests.LocalSync.LocalSyncTestHelpers;

namespace LcmCrdt.Tests.LocalSync;

/// <summary>Stands in for the host's Wi-Fi hardware: "creates" a network on loopback.</summary>
public class FakeAccessPoint : IAccessPoint
{
    public bool Fail { get; set; }
    public bool FailToStop { get; set; }
    public AccessPointCredentials? Running { get; private set; }
    public bool IsSupported => true;

    public Task<AccessPointInfo> Start(AccessPointCredentials network, CancellationToken cancellationToken = default)
    {
        if (Fail) throw new AccessPointUnavailableException(HostingProblem.CantCreateNetwork, "The adapter doesn't support this");
        Running = network;
        return Task.FromResult(new AccessPointInfo(network, IPAddress.Loopback));
    }

    public Task Stop()
    {
        if (FailToStop) throw new InvalidOperationException("The driver hung");
        Running = null;
        return Task.CompletedTask;
    }
}

/// <summary>Stands in for the operating system's neighbor table.</summary>
public class FakeNetworkNeighbors : INetworkNeighbors
{
    public IReadOnlyList<NetworkNeighbor> Devices { get; set; } = [];

    public Task<IReadOnlyList<NetworkNeighbor>> Find(IPAddress hostAddress, CancellationToken cancellationToken = default) =>
        Task.FromResult(Devices);
}

public class HostingNetworkTests : IAsyncLifetime
{
    static HostingNetworkTests()
    {
        // a process-wide switch in the mDNS library; only tests use loopback
        Makaretu.Dns.MulticastService.IncludeLoopbackInterfaces = true;
    }

    private readonly MiniLcmApiFixture _host = CreateFixture(Guid.NewGuid());

    public Task InitializeAsync() => _host.InitializeAsync();

    public async Task DisposeAsync()
    {
        await Hosting.StopHosting();
        await _host.DisposeAsync();
    }

    private LocalSyncHosting Hosting => _host.GetService<LocalSyncHosting>();
    private FakeAccessPoint AccessPoint => (FakeAccessPoint)_host.GetService<IAccessPoint>();
    private FakeNetworkNeighbors Neighbors => (FakeNetworkNeighbors)_host.GetService<INetworkNeighbors>();
    private CrdtProject Project => _host.GetService<CurrentProjectService>().Project;
    private Guid ProjectId => _host.GetService<CurrentProjectService>().ProjectData.Id;

    [Fact]
    public async Task HostingOnItsOwnNetwork_StartsTheAccessPointAndInvitesJoinersOntoIt()
    {
        var hosting = await Hosting.StartHosting(Project, new HostingNetwork.OwnAccessPoint());

        AccessPoint.Running.Should().NotBeNull();
        hosting.Invitation.Network.Should().Be(AccessPoint.Running);
        hosting.Invitation.Host.Should().Be(new Uri($"http://127.0.0.1:{hosting.Port}"));
        hosting.Invitation.ProjectId.Should().Be(ProjectId);
    }

    [Fact]
    public async Task EachSessionGetsANewNetwork()
    {
        var first = (await Hosting.StartHosting(Project, new HostingNetwork.OwnAccessPoint())).Invitation.Network;
        await Hosting.StopHosting();

        var second = (await Hosting.StartHosting(Project, new HostingNetwork.OwnAccessPoint())).Invitation.Network;

        second.Should().NotBe(first);
    }

    [Fact]
    public async Task StoppingTakesTheAccessPointDown()
    {
        await Hosting.StartHosting(Project, new HostingNetwork.OwnAccessPoint());

        await Hosting.StopHosting();

        AccessPoint.Running.Should().BeNull();
    }

    [Fact]
    public async Task WhenTheAccessPointCantStart_NothingIsLeftRunningAndAnExistingNetworkCanBeUsed()
    {
        AccessPoint.Fail = true;

        var act = () => Hosting.StartHosting(Project, new HostingNetwork.OwnAccessPoint());

        await act.Should().ThrowAsync<AccessPointUnavailableException>();
        Hosting.Current.Should().BeNull();
        HostingKeepAwake(_host).Awake.Should().BeFalse();
        var hosting = await Hosting.StartHosting(Project, new HostingNetwork.Existing(IPAddress.Loopback));
        hosting.Invitation.Network.Should().BeNull();
    }

    [Fact]
    public async Task TheServerOnlyListensOnTheSharedNetwork()
    {
        var hosting = await Hosting.StartHosting(Project, new HostingNetwork.Existing(IPAddress.Loopback));
        // another loopback address: same machine, different address than the one being shared
        using var otherAddress = new TcpClient();

        var connect = () => otherAddress.ConnectAsync(IPAddress.Parse("127.0.0.2"), hosting.Port);

        await connect.Should().ThrowAsync<SocketException>();
    }

    [Fact]
    public async Task OnItsOwnNetwork_TheHostListsTheDevicesThatHaventAskedToJoin()
    {
        Neighbors.Devices =
        [
            new NetworkNeighbor(IPAddress.Parse("192.168.137.45"), "field-laptop"),
            new NetworkNeighbor(IPAddress.Parse("192.168.137.46"), null),
            new NetworkNeighbor(IPAddress.Parse("192.168.137.47"), null),
        ];
        Hosting.WatchInterval = TimeSpan.FromMilliseconds(20);
        var hosting = await Hosting.StartHosting(Project, new HostingNetwork.OwnAccessPoint());
        hosting.Session.SayHello("192.168.137.46", new Hello("Amina", AppVersion.Version));
        hosting.Session.RequestJoin(new JoinRequest("Daniel", AppVersion.Version, LocalSyncProtocol.Version, null), address: "192.168.137.47");

        var devices = await Eventually(() => Hosting.Current is { NetworkDevices.Count: > 0 } current
            ? HostingView.From(current, ProjectId).NearbyDevices
            : null);

        devices.Should().Equal(
            new NearbyDeviceView("192.168.137.46", "Amina", HasApp: true, VersionMismatch: false),
            new NearbyDeviceView("192.168.137.45", "field-laptop", HasApp: false, VersionMismatch: false));
    }

    [Fact]
    public async Task WhenTheHostsAddressChanges_ItMovesThereAndKeepsItsJoiners()
    {
        var hosting = await Hosting.StartHosting(Project, new HostingNetwork.Existing(IPAddress.Loopback));
        var requestId = hosting.Session.RequestJoin(new JoinRequest("Amina", AppVersion.Version, LocalSyncProtocol.Version, null));
        hosting.Session.Approve(requestId);

        await Hosting.MoveTo(IPAddress.Parse("127.0.0.2"));

        var moved = Hosting.Current!;
        moved.Invitation.Host.Should().Be(new Uri($"http://127.0.0.2:{hosting.Port}"), "the port is kept when it's free");
        moved.Session.Should().BeSameAs(hosting.Session);
        moved.Session.GetJoinStatus(requestId)!.State.Should().Be(JoinState.Approved);
        using var newAddress = new TcpClient();
        await newAddress.ConnectAsync(IPAddress.Parse("127.0.0.2"), moved.Port);
        using var oldAddress = new TcpClient();
        var connectToOld = () => oldAddress.ConnectAsync(IPAddress.Loopback, hosting.Port);
        await connectToOld.Should().ThrowAsync<SocketException>();
    }

    [Fact]
    public async Task WhenTheAccessPointFailsToStop_TheMachineMaySleepAgainAndHostingCanRestart()
    {
        await Hosting.StartHosting(Project, new HostingNetwork.OwnAccessPoint());
        AccessPoint.FailToStop = true;

        var stop = () => Hosting.StopHosting();

        await stop.Should().ThrowAsync<InvalidOperationException>();
        Hosting.Current.Should().BeNull();
        HostingKeepAwake(_host).Awake.Should().BeFalse();
    }

    [Fact]
    public async Task AFailedMove_LeavesHostingWhereItWas()
    {
        var hosting = await Hosting.StartHosting(Project, new HostingNetwork.Existing(IPAddress.Loopback));
        // not an address of this machine, so nothing can listen there
        var move = () => Hosting.MoveTo(IPAddress.Parse("192.0.2.1"));

        await move.Should().ThrowAsync<SocketException>();

        Hosting.Current!.Invitation.Host.Should().Be(hosting.Invitation.Host);
        using var stillThere = new TcpClient();
        await stillThere.ConnectAsync(IPAddress.Loopback, hosting.Port);
    }

    private static async Task<T> Eventually<T>(Func<T?> get) where T : class
    {
        for (var i = 0; i < 250; i++)
        {
            if (get() is { } value) return value;
            await Task.Delay(20);
        }
        throw new TimeoutException("Timed out waiting");
    }

    [LoopbackMulticastFact]
    public async Task AHostedSessionCanBeFoundOnTheNetwork()
    {
        var hosting = await Hosting.StartHosting(Project, new HostingNetwork.Existing(IPAddress.Loopback));

        var found = await LocalSyncDiscovery.Browse(TimeSpan.FromSeconds(3));

        found.Should().ContainSingle(h => h.ProjectId == ProjectId)
            .Which.Host.Should().Be(new Uri($"http://127.0.0.1:{hosting.Port}"));
    }

    [LoopbackMulticastFact]
    public async Task AStoppedSessionIsNoLongerFound()
    {
        await Hosting.StartHosting(Project, new HostingNetwork.Existing(IPAddress.Loopback));
        await Hosting.StopHosting();

        var found = await LocalSyncDiscovery.Browse(TimeSpan.FromSeconds(3));

        found.Should().NotContain(h => h.ProjectId == ProjectId);
    }
}
