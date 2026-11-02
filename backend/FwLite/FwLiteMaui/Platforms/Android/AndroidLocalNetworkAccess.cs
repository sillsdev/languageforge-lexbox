using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using FwLiteShared.LocalSync.Hosting;
using Microsoft.Extensions.Logging;

namespace FwLiteMaui;

/// <summary>
/// While local sync uses the host's Wi-Fi network: routes the app over it and accepts multicast for mDNS. A host's
/// network has no internet, so Android keeps sending the app's traffic over mobile data unless the process is bound to
/// the Wi-Fi network, and it drops multicast packets unless a multicast lock is held.
/// <para>
/// Android can drop that network (it prefers one with internet) and the user can switch networks, so the binding
/// follows Wi-Fi: it's released when the bound network is lost and made again when a Wi-Fi network is available.
/// </para>
/// </summary>
public sealed class AndroidLocalNetworkAccess(ILogger<AndroidLocalNetworkAccess> logger) : RefCountedLocalNetworkAccess
{
    private WifiManager.MulticastLock? _multicastLock;
    private WifiFollower? _follower;

    protected override void Start()
    {
        var context = Android.App.Application.Context;
        if (context.GetSystemService(Context.WifiService) is WifiManager wifi)
        {
            _multicastLock = wifi.CreateMulticastLock("fwlite-local-sync");
            _multicastLock?.SetReferenceCounted(false);
            _multicastLock?.Acquire();
        }

        if (context.GetSystemService(Context.ConnectivityService) is not ConnectivityManager connectivity) return;
        _follower = new WifiFollower(connectivity, logger);
        // requests ask for internet by default, and the host's network has none
        var wifiRequest = new NetworkRequest.Builder()
            .AddTransportType(TransportType.Wifi)!
            .RemoveCapability(NetCapability.Internet)!
            .Build();
        // reports every Wi-Fi network already connected straight away, then each change
        connectivity.RegisterNetworkCallback(wifiRequest!, _follower);
    }

    protected override void Stop()
    {
        if (_follower is not null)
        {
            _follower.Connectivity.UnregisterNetworkCallback(_follower);
            _follower.Connectivity.BindProcessToNetwork(null);
            _follower = null;
        }
        _multicastLock?.Release();
        _multicastLock = null;
    }

    private sealed class WifiFollower(ConnectivityManager connectivity, ILogger logger) : ConnectivityManager.NetworkCallback
    {
        public ConnectivityManager Connectivity => connectivity;

        public override void OnAvailable(Android.Net.Network network)
        {
            logger.LogInformation("Local sync: routing over the Wi-Fi network {Network}", network);
            connectivity.BindProcessToNetwork(network);
        }

        public override void OnLost(Android.Net.Network network)
        {
            if (!network.Equals(connectivity.BoundNetworkForProcess)) return;
            logger.LogWarning("Local sync: lost the Wi-Fi network {Network}; waiting for Wi-Fi to come back", network);
            connectivity.BindProcessToNetwork(null);
        }
    }
}
