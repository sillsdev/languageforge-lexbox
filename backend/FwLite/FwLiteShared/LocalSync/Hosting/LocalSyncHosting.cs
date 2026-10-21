using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FwLiteShared.KeepAwake;
using LcmCrdt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIL.Harmony.Config;

namespace FwLiteShared.LocalSync.Hosting;

/// <summary>Which network a host shares its project over.</summary>
public abstract record HostingNetwork
{
    /// <summary>A Wi-Fi network this device creates, so no router is needed. Its name and password are new each session.</summary>
    public sealed record OwnAccessPoint : HostingNetwork;

    /// <summary>A network this device is already on (e.g. a phone's hotspot it joined), at this device's address on it.</summary>
    public sealed record Existing(IPAddress Address) : HostingNetwork;
}

/// <summary>A network this device is on, which a host can share over when it can't create its own.</summary>
public record AvailableNetwork(string Name, IPAddress Address);

/// <param name="Invitation">What the hosting panel's QR code shows.</param>
public record HostingInfo(HostSession Session, int Port, JoinInvitation Invitation);

/// <summary>
/// Starts and stops sharing one project for local sync: the network (creating one if asked), the session and its HTTP
/// server listening only on that network, the mDNS announcement, and keeping the machine awake for as long as it runs.
/// At most one project is hosted at a time.
/// </summary>
public class LocalSyncHosting(
    IServiceProvider services,
    IKeepAwake keepAwake,
    IAccessPoint accessPoint,
    IOptions<HarmonyConfig> harmonyConfig,
    ILogger<LocalSyncHosting> logger) : IAsyncDisposable
{
    /// <summary>Tried first, so a joiner typing an address by hand usually sees the same port; any free port otherwise.</summary>
    public const int DefaultPort = 5139;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private Running? _running;

    private sealed record Running(
        HostingInfo Info,
        LocalSyncHttpServer Server,
        LocalSyncAdvertiser Advertiser,
        bool OwnsAccessPoint,
        TaskCompletionSource Ended,
        Task KeepAwake);

    public HostingInfo? Current => _running?.Info;

    /// <summary>Whether this device can try to create its own network (it can still fail; then offer <see cref="AvailableNetworks"/>).</summary>
    public bool CanCreateNetwork => accessPoint.IsSupported;

    /// <exception cref="AccessPointUnavailableException">The device couldn't create its own network; nothing is left running.</exception>
    public async Task<HostingInfo> StartHosting(CrdtProject project, HostingNetwork network)
    {
        await _lock.WaitAsync();
        try
        {
            if (_running is not null) throw new InvalidOperationException("Already hosting a project; stop hosting it first");
            var hosted = await HostedProjectReader.Read(services, project);

            AccessPointInfo? created = null;
            if (network is HostingNetwork.OwnAccessPoint) created = await accessPoint.Start(AccessPointCredentials.Generate());
            var address = created?.HostAddress ?? ((HostingNetwork.Existing)network).Address;
            try
            {
                _running = Start(project, hosted, address, created);
                return _running.Info;
            }
            catch
            {
                if (created is not null) await accessPoint.Stop();
                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private Running Start(CrdtProject project, HostedProject hosted, IPAddress address, AccessPointInfo? created)
    {
        var session = new HostSession(hosted, AppVersion.Version);
        var port = IsFree(address, DefaultPort) ? DefaultPort : FreePort(address);
        var server = new LocalSyncHttpServer(session, project, services,
            harmonyConfig.Value.MakeLcmCrdtExternalJsonOptions(), logger, address, port);
        server.Start();
        LocalSyncAdvertiser advertiser;
        try
        {
            advertiser = LocalSyncAdvertiser.Start(
                new AdvertisedSession(hosted.ProjectId, hosted.ProjectName, AppVersion.Version, LocalSyncProtocol.Version, port), address);
        }
        catch
        {
            server.Dispose();
            throw;
        }

        var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var awake = keepAwake.RunAsync(new KeepAwakeWork($"Sharing {hosted.ProjectName} for local sync"),
            async () => await ended.Task);
        var invitation = new JoinInvitation(new Uri($"http://{address}:{port}"), hosted.ProjectId, hosted.ProjectName, created?.Network);
        logger.LogInformation("Hosting {Project} for local sync on {Address}:{Port}", project.Name, address, port);
        return new Running(new HostingInfo(session, port, invitation), server, advertiser, created is not null, ended, awake);
    }

    /// <summary>
    /// Ends the session (revoking every joiner), withdraws the announcement, stops the server, takes down the network
    /// if this device created it, and lets the machine sleep again.
    /// </summary>
    public async Task StopHosting()
    {
        await _lock.WaitAsync();
        try
        {
            if (_running is not { } running) return;
            _running = null;
            running.Info.Session.End();
            running.Advertiser.Dispose();
            running.Server.Dispose();
            if (running.OwnsAccessPoint) await accessPoint.Stop();
            running.Ended.TrySetResult();
            await running.KeepAwake;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// The networks this device is on that other devices could reach, for sharing over one of them when it can't
    /// create its own. Wi-Fi comes first. Adapters for virtual machines and containers are left out, as are addresses
    /// still waiting for DHCP.
    /// </summary>
    public static IReadOnlyList<AvailableNetwork> AvailableNetworks() =>
    [
        .. NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                          && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
                          && !IsVirtual(nic))
            .OrderBy(nic => nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? 0 : 1)
            .SelectMany(nic => nic.GetIPProperties().UnicastAddresses
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork
                            && LocalAddresses.IsLocal(a.Address)
                            && !IPAddress.IsLoopback(a.Address)
                            && !LocalAddresses.IsLinkLocal(a.Address))
                .Select(a => new AvailableNetwork(nic.Name, a.Address)))
    ];

    private static readonly string[] VirtualAdapterMarkers = ["Virtual", "vEthernet", "VMware", "VirtualBox", "Hyper-V", "docker", "WSL", "veth", "virbr"];

    private static bool IsVirtual(NetworkInterface nic) =>
        VirtualAdapterMarkers.Any(marker => nic.Name.Contains(marker, StringComparison.OrdinalIgnoreCase)
                                            || nic.Description.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static bool IsFree(IPAddress address, int port)
    {
        try
        {
            using var listener = new TcpListener(address, port);
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static int FreePort(IPAddress address)
    {
        using var listener = new TcpListener(address, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    public async ValueTask DisposeAsync() => await StopHosting();
}
