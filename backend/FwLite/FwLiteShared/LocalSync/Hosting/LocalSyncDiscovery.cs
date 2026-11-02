using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using Makaretu.Dns;

namespace FwLiteShared.LocalSync.Hosting;

/// <summary>What a host announces on the network, so joiners can list it without scanning a QR code.</summary>
public record AdvertisedSession(Guid ProjectId, string ProjectName, string AppVersion, int ProtocolVersion, int Port);

/// <param name="Host">The host's sync address, e.g. <c>http://192.168.137.1:5139</c>.</param>
public record DiscoveredHost(Uri Host, Guid ProjectId, string ProjectName, string AppVersion, int ProtocolVersion);

internal static class LocalSyncDns
{
    public const string ServiceName = "_fwlite-sync._tcp";
    public const string ProjectKey = "project";
    public const string NameKey = "name";
    public const string AppVersionKey = "app";
    public const string ProtocolKey = "protocol";

    /// <summary>The network interfaces that own <paramref name="address"/>, so nothing is announced on other networks.</summary>
    public static IEnumerable<NetworkInterface> InterfacesWith(IEnumerable<NetworkInterface> interfaces, IPAddress address) =>
        interfaces.Where(nic => nic.GetIPProperties().UnicastAddresses.Any(a => a.Address.Equals(address)));
}

/// <summary>
/// Announces a hosting session with mDNS (DNS-SD service <c>_fwlite-sync._tcp</c>) on the network the host shares
/// over, and only that one. Disposing withdraws the announcement.
/// </summary>
public sealed class LocalSyncAdvertiser : IDisposable
{
    private readonly MulticastService _mdns;
    private readonly ServiceDiscovery _discovery;
    private readonly ServiceProfile _profile;

    private LocalSyncAdvertiser(AdvertisedSession session, IPAddress address)
    {
        _mdns = new MulticastService(interfaces => LocalSyncDns.InterfacesWith(interfaces, address)) { UseIpv6 = false };
        _discovery = new ServiceDiscovery(_mdns);
        // unique per session, so two hosts sharing the same project are both listed
        var instanceName = "FWLite-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
        _profile = new ServiceProfile(instanceName, LocalSyncDns.ServiceName, (ushort)session.Port, [address]);
        _profile.AddProperty(LocalSyncDns.ProjectKey, session.ProjectId.ToString());
        _profile.AddProperty(LocalSyncDns.NameKey, session.ProjectName);
        _profile.AddProperty(LocalSyncDns.AppVersionKey, session.AppVersion);
        _profile.AddProperty(LocalSyncDns.ProtocolKey, session.ProtocolVersion.ToString());
    }

    /// <param name="address">The host's address on the network it shares over (its access point's, usually).</param>
    public static LocalSyncAdvertiser Start(AdvertisedSession session, IPAddress address)
    {
        var advertiser = new LocalSyncAdvertiser(session, address);
        try
        {
            advertiser._mdns.Start();
            advertiser._discovery.Advertise(advertiser._profile);
            advertiser._discovery.Announce(advertiser._profile);
            return advertiser;
        }
        catch
        {
            advertiser.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        _discovery.Unadvertise(_profile);
        _discovery.Dispose();
        _mdns.Stop();
        _mdns.Dispose();
    }
}

public static class LocalSyncDiscovery
{
    private static readonly TimeSpan QueryEvery = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Lists the hosts announcing a session on the networks this device is on. Queries are repeated while browsing,
    /// because multicast packets get lost, so a host that's slow to answer still shows up.
    /// </summary>
    public static async Task<IReadOnlyList<DiscoveredHost>> Browse(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var found = new ConcurrentDictionary<string, DiscoveredHost>();
        using var mdns = new MulticastService { UseIpv6 = false };
        using var discovery = new ServiceDiscovery(mdns);
        discovery.ServiceInstanceDiscovered += (_, e) =>
        {
            if (Read(e.ServiceInstanceName, e.Message) is { } host) found[e.ServiceInstanceName.ToString()] = host;
        };
        discovery.ServiceInstanceShutdown += (_, e) => found.TryRemove(e.ServiceInstanceName.ToString(), out DiscoveredHost? _);
        mdns.Start();
        try
        {
            var deadline = DateTimeOffset.UtcNow + duration;
            while (!cancellationToken.IsCancellationRequested)
            {
                discovery.QueryServiceInstances(LocalSyncDns.ServiceName);
                // measured once: the deadline can pass between a check and a second reading of the clock
                var left = deadline - DateTimeOffset.UtcNow;
                if (left <= TimeSpan.Zero) break;
                await Task.Delay(left < QueryEvery ? left : QueryEvery, cancellationToken);
            }
        }
        finally
        {
            mdns.Stop();
        }
        return [.. found.Values];
    }

    /// <summary>Reads a host from an answer that carries the instance's SRV, TXT and address records.</summary>
    private static DiscoveredHost? Read(DomainName instance, Message message)
    {
        var records = message.Answers.Concat(message.AdditionalRecords).ToArray();
        var srv = records.OfType<SRVRecord>().FirstOrDefault(r => r.Name == instance);
        var txt = records.OfType<TXTRecord>().FirstOrDefault(r => r.Name == instance);
        if (srv is null || txt is null) return null;
        var address = records.OfType<ARecord>().FirstOrDefault(r => r.Name == srv.Target)?.Address;
        if (address is null || address.AddressFamily != AddressFamily.InterNetwork) return null;

        var properties = txt.Strings
            .Select(s => s.Split('=', 2))
            .Where(kv => kv.Length == 2)
            .GroupBy(kv => kv[0])
            .ToDictionary(g => g.Key, g => g.First()[1]);
        if (!Guid.TryParse(properties.GetValueOrDefault(LocalSyncDns.ProjectKey), out var projectId)) return null;
        _ = int.TryParse(properties.GetValueOrDefault(LocalSyncDns.ProtocolKey), out var protocolVersion);
        return new DiscoveredHost(new Uri($"http://{address}:{srv.Port}"), projectId,
            properties.GetValueOrDefault(LocalSyncDns.NameKey) ?? "",
            properties.GetValueOrDefault(LocalSyncDns.AppVersionKey) ?? "",
            protocolVersion);
    }
}
