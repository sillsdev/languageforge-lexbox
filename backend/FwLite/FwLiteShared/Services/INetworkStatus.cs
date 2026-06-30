namespace FwLiteShared.Services;

/// <summary>
/// What the device believes about its own connectivity. Hosts without a richer connectivity API
/// (e.g. FwLiteWeb) use <see cref="NetworkInterfaceNetworkStatus"/>.
/// </summary>
public interface INetworkStatus
{
    bool IsOnline { get; }
}
