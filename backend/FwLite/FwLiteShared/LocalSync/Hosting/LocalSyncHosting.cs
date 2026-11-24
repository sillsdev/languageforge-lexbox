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
public record HostingInfo(HostSession Session, int Port, JoinInvitation Invitation)
{
    /// <summary>
    /// The devices connected to the network the host created, whether or not they have the app open; refreshed every
    /// <see cref="LocalSyncHosting.WatchInterval"/>. Empty when sharing over a network the host was already on.
    /// </summary>
    public IReadOnlyList<NetworkNeighbor> NetworkDevices { get; init; } = [];
}

/// <summary>
/// Starts and stops sharing one project for local sync: the network (creating one if asked), the session and its HTTP
/// server listening only on that network, the mDNS announcement, and keeping the machine awake for as long as it runs.
/// While it runs it follows the host's address (a network the host was already on can hand it a new one) and lists the
/// devices on a network it created. At most one project is hosted at a time.
/// </summary>
public class LocalSyncHosting(
    IServiceProvider services,
    IKeepAwake keepAwake,
    IAccessPoint accessPoint,
    INetworkNeighbors networkNeighbors,
    IOptions<HarmonyConfig> harmonyConfig,
    ILogger<LocalSyncHosting> logger) : IAsyncDisposable
{
    /// <summary>Tried first, so a joiner typing an address by hand usually sees the same port; any free port otherwise.</summary>
    public const int DefaultPort = 5139;

    /// <summary>The port tried first; 0 to take any free port.</summary>
    public int PreferredPort { get; set; } = DefaultPort;

    /// <summary>How often the host checks its address and refreshes the list of devices on its network.</summary>
    public TimeSpan WatchInterval { get; set; } = TimeSpan.FromSeconds(10);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private volatile Running? _running;

    /// <param name="InterfaceId">The network adapter the host shares over, to find its new address if it changes.</param>
    private sealed record Running(
        HostingInfo Info,
        CrdtProject Project,
        HostedProject Hosted,
        string? InterfaceId,
        LocalSyncHttpServer Server,
        LocalSyncAdvertiser Advertiser,
        bool OwnsAccessPoint,
        TaskCompletionSource Ended,
        Task KeepAwake,
        CancellationTokenSource StopWatching)
    {
        public IPAddress Address => IPAddress.Parse(Info.Invitation.Host.Host);
    }

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
        var (server, advertiser, port) = Listen(session, project, hosted, address, PreferredPort);

        var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var awake = keepAwake.RunAsync(new KeepAwakeWork($"Sharing {hosted.ProjectName} for local sync"),
            async () => await ended.Task);
        var invitation = new JoinInvitation(new Uri($"http://{address}:{port}"), hosted.ProjectId, hosted.ProjectName, created?.Network);
        logger.LogInformation("Hosting {Project} for local sync on {Address}:{Port}", project.Name, address, port);
        var stopWatching = new CancellationTokenSource();
        var running = new Running(new HostingInfo(session, port, invitation), project, hosted, InterfaceIdOf(address),
            server, advertiser, created is not null, ended, awake, stopWatching);
        _ = Task.Run(async () => await Watch(stopWatching.Token));
        return running;
    }

    /// <summary>Starts the HTTP server and the mDNS announcement on <paramref name="address"/>.</summary>
    private (LocalSyncHttpServer Server, LocalSyncAdvertiser Advertiser, int Port) Listen(
        HostSession session, CrdtProject project, HostedProject hosted, IPAddress address, int preferredPort)
    {
        var port = preferredPort != 0 && IsFree(address, preferredPort) ? preferredPort : FreePort(address);
        var server = new LocalSyncHttpServer(session, project, services,
            harmonyConfig.Value.MakeLcmCrdtSyncJsonOptions(), logger, address, port);
        server.Start();
        try
        {
            var advertiser = LocalSyncAdvertiser.Start(
                new AdvertisedSession(hosted.ProjectId, hosted.ProjectName, AppVersion.Version, LocalSyncProtocol.Version, port), address);
            return (server, advertiser, port);
        }
        catch
        {
            server.Dispose();
            throw;
        }
    }

    private async Task Watch(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await FollowAddress();
                await RefreshNetworkDevices(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Watching the local sync host's network failed");
            }
            try
            {
                await Task.Delay(WatchInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// A network the host was already on can give it a new address (DHCP renewal, roaming between access points). The
    /// server listens on one address only, so when that address is gone it moves to the adapter's new one. Joiners
    /// find it again through mDNS.
    /// </summary>
    private async Task FollowAddress()
    {
        if (_running is not { OwnsAccessPoint: false, InterfaceId: { } interfaceId } running) return;
        if (InterfaceIdOf(running.Address) is not null) return;
        var newAddress = AddressOf(interfaceId);
        if (newAddress is null)
        {
            logger.LogWarning("The local sync host lost its address {Address} and has no new one yet", running.Address);
            return;
        }
        await MoveTo(newAddress);
    }

    /// <summary>Moves the server and announcement to <paramref name="address"/>, keeping the session and its joiners.</summary>
    public async Task MoveTo(IPAddress address)
    {
        await _lock.WaitAsync();
        try
        {
            if (_running is not { } running || running.Address.Equals(address)) return;
            running.Advertiser.Dispose();
            running.Server.Dispose();
            var (server, advertiser, port) = Listen(running.Info.Session, running.Project, running.Hosted, address, running.Info.Port);
            var invitation = running.Info.Invitation with { Host = new Uri($"http://{address}:{port}") };
            logger.LogInformation("The local sync host's address changed from {OldAddress} to {Address}:{Port}",
                running.Address, address, port);
            _running = running with
            {
                Info = running.Info with { Port = port, Invitation = invitation },
                Server = server,
                Advertiser = advertiser,
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RefreshNetworkDevices(CancellationToken cancellationToken)
    {
        if (_running is not { OwnsAccessPoint: true } running) return;
        var devices = await networkNeighbors.Find(running.Address, cancellationToken);
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_running is { } current && current.Info.Session == running.Info.Session)
                _running = current with { Info = current.Info with { NetworkDevices = devices } };
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string? InterfaceIdOf(IPAddress address) =>
        NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(nic => nic.OperationalStatus == OperationalStatus.Up
                                   && nic.GetIPProperties().UnicastAddresses.Any(a => a.Address.Equals(address)))
            ?.Id;

    private static IPAddress? AddressOf(string interfaceId) =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.Id == interfaceId && nic.OperationalStatus == OperationalStatus.Up)
            .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
            .Select(a => a.Address)
            .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !LocalAddresses.IsLinkLocal(a));

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
            await running.StopWatching.CancelAsync();
            running.StopWatching.Dispose();
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
