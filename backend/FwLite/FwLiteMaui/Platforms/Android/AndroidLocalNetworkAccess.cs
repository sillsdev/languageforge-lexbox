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
/// </summary>
public sealed class AndroidLocalNetworkAccess(ILogger<AndroidLocalNetworkAccess> logger) : RefCountedLocalNetworkAccess
{
    private WifiManager.MulticastLock? _multicastLock;

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
#pragma warning disable CA1422 // GetAllNetworks is deprecated from API 31 but still the simplest way to find the Wi-Fi network
        var wifiNetwork = connectivity.GetAllNetworks()
            .FirstOrDefault(n => connectivity.GetNetworkCapabilities(n)?.HasTransport(TransportType.Wifi) == true);
#pragma warning restore CA1422
        if (wifiNetwork is null)
        {
            logger.LogWarning("Not connected to Wi-Fi, so local sync can't reach the host");
            return;
        }
        connectivity.BindProcessToNetwork(wifiNetwork);
    }

    protected override void Stop()
    {
        if (Android.App.Application.Context.GetSystemService(Context.ConnectivityService) is ConnectivityManager connectivity)
            connectivity.BindProcessToNetwork(null);
        _multicastLock?.Release();
        _multicastLock = null;
    }
}
