using FwLiteShared.Services;

namespace FwLiteMaui.Services;

public class ConnectivityNetworkStatus(IConnectivity connectivity) : INetworkStatus
{
    // ConstrainedInternet (e.g. captive portal) counts as offline, matching ConnectivitySyncTrigger.
    public bool IsOnline => connectivity.NetworkAccess == NetworkAccess.Internet;
}
