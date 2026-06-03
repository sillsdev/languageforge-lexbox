namespace FwLiteShared.Services;

/// <summary>
/// What the device believes about its own connectivity. Hosts without a connectivity API
/// (e.g. FwLiteWeb) use <see cref="AlwaysOnlineNetworkStatus"/>.
/// </summary>
public interface INetworkStatus
{
    bool IsOnline { get; }
}

public class AlwaysOnlineNetworkStatus : INetworkStatus
{
    public bool IsOnline => true;
}
