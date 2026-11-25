using System.Net.Sockets;
using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using FwLiteShared.LocalSync.Hosting;
using Microsoft.Extensions.Logging;

namespace FwLiteMaui;

/// <summary>
/// While local sync uses the host's Wi-Fi network: binds local sync's sockets to it and accepts multicast for mDNS. A
/// host's network has no internet, so Android sends the app's traffic over mobile data unless a socket is bound to the
/// Wi-Fi network, and it drops multicast packets unless a multicast lock is held.
/// <para>
/// Android can drop that network (it prefers one with internet) and the user can switch networks, so the network used
/// follows Wi-Fi: it's forgotten when lost and picked up again when a Wi-Fi network is available.
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
        _follower = new WifiFollower(connectivity, logger) { Current = ConnectedWifi(connectivity) };
        // requests ask for internet by default, and the host's network has none
        var wifiRequest = new NetworkRequest.Builder()
            .AddTransportType(TransportType.Wifi)!
            .RemoveCapability(NetCapability.Internet)!
            .Build();
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

    protected override void RouteApp(bool overLocalNetwork)
    {
        if (_follower is null) return;
        _follower.RoutingApp = overLocalNetwork;
        _follower.Connectivity.BindProcessToNetwork(overLocalNetwork ? _follower.Current : null);
    }

    public override void Bind(Socket socket)
    {
        if (_follower?.Current is not { } network) return;
        // the dup shares the socket, so binding it binds ours
        using var descriptor = ParcelFileDescriptor.FromFd((int)socket.Handle)
                               ?? throw new InvalidOperationException("Android couldn't duplicate the socket");
        network.BindSocket(descriptor.FileDescriptor);
    }

    // registering the callback reports networks asynchronously, so the first sockets would miss the one already connected
    private static Android.Net.Network? ConnectedWifi(ConnectivityManager connectivity) =>
        connectivity.GetAllNetworks().FirstOrDefault(network =>
            connectivity.GetNetworkCapabilities(network)?.HasTransport(TransportType.Wifi) == true);

    private sealed class WifiFollower(ConnectivityManager connectivity, ILogger logger) : ConnectivityManager.NetworkCallback
    {
        public ConnectivityManager Connectivity => connectivity;
        public volatile Android.Net.Network? Current;
        public volatile bool RoutingApp;

        public override void OnAvailable(Android.Net.Network network)
        {
            logger.LogInformation("Local sync: using the Wi-Fi network {Network}", network);
            Current = network;
            if (RoutingApp) connectivity.BindProcessToNetwork(network);
        }

        public override void OnLost(Android.Net.Network network)
        {
            if (!network.Equals(Current)) return;
            logger.LogWarning("Local sync: lost the Wi-Fi network {Network}; waiting for Wi-Fi to come back", network);
            Current = null;
            if (RoutingApp) connectivity.BindProcessToNetwork(null);
        }
    }
}
