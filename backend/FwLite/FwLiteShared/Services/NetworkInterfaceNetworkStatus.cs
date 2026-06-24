using System.Net.NetworkInformation;

namespace FwLiteShared.Services;

/// <summary>
/// Reports connectivity from the OS network-interface table via
/// <see cref="NetworkInterface.GetIsNetworkAvailable"/>. The signal is asymmetric: <see cref="IsOnline"/>
/// false is authoritative (no non-loopback interface is up, so the device is offline), but true only means
/// an interface is up — not that the internet is reachable. A virtual adapter (VPN, WSL, Hyper-V) or a LAN
/// with a dead uplink still reads as online. Suitable for hosts without a richer connectivity API.
/// </summary>
public class NetworkInterfaceNetworkStatus : INetworkStatus
{
    public bool IsOnline => NetworkInterface.GetIsNetworkAvailable();
}
