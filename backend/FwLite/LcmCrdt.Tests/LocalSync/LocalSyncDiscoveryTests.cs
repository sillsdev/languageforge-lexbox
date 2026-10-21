using System.Net;
using System.Net.NetworkInformation;
using FwLiteShared.LocalSync.Hosting;

namespace LcmCrdt.Tests.LocalSync;

/// <summary>
/// Real mDNS on the loopback interface: a host advertises, a browser on the same machine finds it.
/// </summary>
public class LocalSyncDiscoveryTests
{
    static LocalSyncDiscoveryTests()
    {
        // a process-wide switch in the mDNS library; only tests use loopback
        Makaretu.Dns.MulticastService.IncludeLoopbackInterfaces = true;
    }

    private static readonly TimeSpan BrowseFor = TimeSpan.FromSeconds(3);

    private static AdvertisedSession Session(Guid projectId, string projectName = "Sena 3") =>
        new(projectId, projectName, AppVersion: "1.4.0", LocalSyncProtocol.Version, Port: 5139);

    [LoopbackMulticastFact]
    public async Task AnAdvertisedHostIsFound()
    {
        var projectId = Guid.NewGuid();
        using var advertiser = LocalSyncAdvertiser.Start(Session(projectId, "Sena 3 & friends"), IPAddress.Loopback);

        var found = await LocalSyncDiscovery.Browse(BrowseFor);

        var host = found.Should().ContainSingle(h => h.ProjectId == projectId).Which;
        host.Host.Should().Be(new Uri("http://127.0.0.1:5139"));
        host.ProjectName.Should().Be("Sena 3 & friends");
        host.AppVersion.Should().Be("1.4.0");
        host.ProtocolVersion.Should().Be(LocalSyncProtocol.Version);
    }

    [LoopbackMulticastFact]
    public async Task AHostThatStoppedAdvertising_IsNotFound()
    {
        var projectId = Guid.NewGuid();
        LocalSyncAdvertiser.Start(Session(projectId), IPAddress.Loopback).Dispose();

        var found = await LocalSyncDiscovery.Browse(BrowseFor);

        found.Should().NotContain(h => h.ProjectId == projectId);
    }
}

/// <summary>
/// Runs only where the loopback interface can multicast: Windows can, but Linux's <c>lo</c> usually can't (as on CI),
/// and there's no other network to test on.
/// </summary>
public sealed class LoopbackMulticastFactAttribute : FactAttribute
{
    public LoopbackMulticastFactAttribute()
    {
        var loopbackMulticasts = NetworkInterface.GetAllNetworkInterfaces().Any(nic =>
            nic.NetworkInterfaceType == NetworkInterfaceType.Loopback && nic.SupportsMulticast && nic.OperationalStatus == OperationalStatus.Up);
        if (!loopbackMulticasts) Skip = "The loopback interface can't multicast here";
    }
}
