using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.LocalSync.Hosting;

/// <summary>A device connected to the host's network, as the network sees it.</summary>
/// <param name="Name">The name it gave the host's DHCP server, when there is one (Windows only, and phones rarely give one).</param>
public record NetworkNeighbor(IPAddress Address, string? Name);

/// <summary>
/// Lists the devices connected to the network a host shares over, including ones that haven't opened the app.
/// </summary>
public interface INetworkNeighbors
{
    /// <summary>The devices on <paramref name="hostAddress"/>'s subnet, apart from the host itself.</summary>
    Task<IReadOnlyList<NetworkNeighbor>> Find(IPAddress hostAddress, CancellationToken cancellationToken = default);
}

/// <summary>
/// Reads the operating system's neighbor (ARP) table. A device only appears there once the host has exchanged
/// packets with it, so a small subnet (a network the host created) is swept first with one UDP packet per address:
/// sending it makes the host look each address up, whether or not the device answers the packet.
/// </summary>
internal sealed class SystemNetworkNeighbors(ILogger<SystemNetworkNeighbors> logger) : INetworkNeighbors
{
    /// <summary>Subnets larger than a /24 (an office network) aren't swept or listed.</summary>
    private const int SmallestPrefix = 24;
    private const int DiscardPort = 9;
    private static readonly TimeSpan SweepSettle = TimeSpan.FromSeconds(1);

    public async Task<IReadOnlyList<NetworkNeighbor>> Find(IPAddress hostAddress, CancellationToken cancellationToken = default)
    {
        if (Subnet.Of(hostAddress) is not { PrefixLength: >= SmallestPrefix } subnet) return [];
        try
        {
            Sweep(subnet);
            await Task.Delay(SweepSettle, cancellationToken);
            var names = OperatingSystem.IsWindows() ? WindowsDhcpNames() : new Dictionary<IPAddress, string>();
            return [.. NeighborTable()
                .Where(subnet.Contains)
                .Distinct()
                .Select(address => new NetworkNeighbor(address, names.GetValueOrDefault(address)))];
        }
        catch (Exception e) when (e is SocketException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(e, "Couldn't list the devices on {Address}'s network", hostAddress);
            return [];
        }
    }

    private static void Sweep(Subnet subnet)
    {
        using var udp = new UdpClient(new IPEndPoint(subnet.HostAddress, 0));
        byte[] probe = [0];
        foreach (var address in subnet.Addresses())
        {
            try
            {
                udp.Send(probe, new IPEndPoint(address, DiscardPort));
            }
            catch (SocketException)
            {
                // an address the OS won't send to; the rest still get swept
            }
        }
    }

    private static IEnumerable<IPAddress> NeighborTable()
    {
        if (OperatingSystem.IsWindows()) return WindowsNeighborTable();
        if (OperatingSystem.IsLinux() && File.Exists(ProcNetArp)) return ParseProcNetArp(File.ReadAllText(ProcNetArp));
        return [];
    }

    private const string ProcNetArp = "/proc/net/arp";
    private const int ArpComplete = 0x2;

    /// <summary>Reads Linux's <c>/proc/net/arp</c>: one row per neighbor, complete ones flagged 0x2.</summary>
    internal static IEnumerable<IPAddress> ParseProcNetArp(string table) =>
        table.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(columns => columns.Length >= 4
                              && (Convert.ToInt32(columns[2], 16) & ArpComplete) != 0
                              && IPAddress.TryParse(columns[0], out _))
            .Select(columns => IPAddress.Parse(columns[0]));

    /// <summary>
    /// Windows' DHCP server for a network the host created records the name each device gave in <c>hosts.ics</c>,
    /// one <c>address name</c> line per device.
    /// </summary>
    private static Dictionary<IPAddress, string> WindowsDhcpNames()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts.ics");
        return File.Exists(path) ? ParseHostsFile(File.ReadAllText(path)) : [];
    }

    internal static Dictionary<IPAddress, string> ParseHostsFile(string hosts)
    {
        var names = new Dictionary<IPAddress, string>();
        foreach (var line in hosts.Split('\n'))
        {
            var content = line.Split('#', 2)[0];
            var columns = content.Split((char[]) [' ', '\t', '\r'], StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length >= 2 && IPAddress.TryParse(columns[0], out var address)) names[address] = columns[1];
        }
        return names;
    }

    // MIB_IPNET_TABLE2 / MIB_IPNET_ROW2 from netioapi.h, read by offset
    private const ushort AfInet = 2;
    private const int TableOffset = 8;
    private const int RowSize = 88;
    private const int AddressOffset = 4; // sin_addr inside the SOCKADDR_INET at the start of the row
    private const int PhysicalAddressLengthOffset = 72;
    private const int StateOffset = 76;

    private enum NeighborState
    {
        Unreachable,
        Incomplete,
        Probe,
        Delay,
        Stale,
        Reachable,
        Permanent,
    }

    [DllImport("iphlpapi.dll")]
    private static extern int GetIpNetTable2(ushort family, out IntPtr table);

    [DllImport("iphlpapi.dll")]
    private static extern void FreeMibTable(IntPtr memory);

    private static List<IPAddress> WindowsNeighborTable()
    {
        if (GetIpNetTable2(AfInet, out var table) != 0) return [];
        try
        {
            var count = Marshal.ReadInt32(table);
            var addresses = new List<IPAddress>(count);
            for (var i = 0; i < count; i++)
            {
                var row = table + TableOffset + i * RowSize;
                if ((ushort)Marshal.ReadInt16(row) != AfInet) continue;
                // permanent rows are broadcast and multicast addresses, not devices
                if ((NeighborState)Marshal.ReadInt32(row + StateOffset) is not (NeighborState.Reachable or NeighborState.Stale
                    or NeighborState.Delay or NeighborState.Probe)) continue;
                if (Marshal.ReadInt32(row + PhysicalAddressLengthOffset) == 0) continue;
                var bytes = new byte[4];
                Marshal.Copy(row + AddressOffset, bytes, 0, bytes.Length);
                addresses.Add(new IPAddress(bytes));
            }
            return addresses;
        }
        finally
        {
            FreeMibTable(table);
        }
    }
}

/// <summary>An IPv4 subnet, seen from one host's address on it.</summary>
internal record Subnet(IPAddress HostAddress, int PrefixLength)
{
    private uint Mask => PrefixLength == 0 ? 0 : uint.MaxValue << (32 - PrefixLength);
    private uint Network => ToUInt(HostAddress) & Mask;
    private uint Broadcast => Network | ~Mask;

    public static Subnet? Of(IPAddress hostAddress) =>
        NetworkInterface.GetAllNetworkInterfaces()
            .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.Equals(hostAddress))
            .Select(a => new Subnet(hostAddress, a.PrefixLength))
            .FirstOrDefault();

    /// <summary>Another device's address on this subnet: not the host's, the network's or the broadcast address.</summary>
    public bool Contains(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork || address.Equals(HostAddress)) return false;
        var value = ToUInt(address);
        return (value & Mask) == Network && value != Network && value != Broadcast;
    }

    /// <summary>Every address another device could have on this subnet.</summary>
    public IEnumerable<IPAddress> Addresses()
    {
        for (var value = Network + 1; value < Broadcast; value++)
        {
            var address = FromUInt(value);
            if (!address.Equals(HostAddress)) yield return address;
        }
    }

    private static uint ToUInt(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);
    }

    private static IPAddress FromUInt(uint value) =>
        new([(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value]);
}
