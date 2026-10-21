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
    public AccessPointCredentials? Running { get; private set; }
    public bool IsSupported => true;

    public Task<AccessPointInfo> Start(AccessPointCredentials network, CancellationToken cancellationToken = default)
    {
        if (Fail) throw new AccessPointUnavailableException("The adapter doesn't support this");
        Running = network;
        return Task.FromResult(new AccessPointInfo(network, IPAddress.Loopback));
    }

    public Task Stop()
    {
        Running = null;
        return Task.CompletedTask;
    }
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
        ((RecordingKeepAwakePlatform)_host.GetService<IKeepAwakePlatform>()).Awake.Should().BeFalse();
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
