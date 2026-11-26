using System.Net;
using System.Net.Sockets;

namespace FwLiteShared.LocalSync.Hosting;

public static class LocalAddresses
{
    /// <summary>An address on a local network: private (RFC 1918), link-local or loopback, never one on the internet.</summary>
    public static bool IsLocal(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return true;
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            return address.IsIPv6LinkLocal || address.IsIPv6UniqueLocal;
        return address.GetAddressBytes() switch
        {
            [10, ..] => true,
            [172, >= 16 and <= 31, ..] => true,
            [192, 168, ..] => true,
            [169, 254, ..] => true,
            _ => false,
        };
    }

    /// <summary>A 169.254.x.x placeholder an adapter has until it gets a real address.</summary>
    public static bool IsLinkLocal(IPAddress address) => address.GetAddressBytes() is [169, 254, ..];
}
