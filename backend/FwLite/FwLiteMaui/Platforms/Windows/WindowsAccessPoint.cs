using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FwLiteShared.LocalSync.Hosting;
using Microsoft.Extensions.Logging;
using Windows.Devices.Radios;
using Windows.Devices.WiFiDirect;
using Windows.Security.Credentials;

namespace FwLiteMaui;

/// <summary>
/// A Wi-Fi Direct group owner acting as a plain WPA2 access point ("legacy" mode), so phones and laptops join it like
/// any network and never negotiate Wi-Fi Direct. Unlike Windows' Mobile Hotspot, it starts with no internet connection
/// to share. Most adapters can run it while staying connected to another network.
/// Wi-Fi Direct connection requests are deliberately not accepted: legacy clients join with the passphrase and need no
/// acceptance, while accepting a Wi-Fi Direct device would let it in by pairing, without the passphrase.
/// </summary>
public sealed class WindowsAccessPoint(ILogger<WindowsAccessPoint> logger) : IAccessPoint
{
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(15);
    private WiFiDirectAdvertisementPublisher? _publisher;

    public bool IsSupported => true;

    public async Task<AccessPointInfo> Start(AccessPointCredentials network, CancellationToken cancellationToken = default)
    {
        await Stop();
        // Windows reports a switched-off radio as an aborted start whose error is "Success", so check first
        if (await IsWifiOff()) throw new AccessPointUnavailableException(WifiOffMessage);
        var started = new TaskCompletionSource<WiFiDirectAdvertisementPublisherStatusChangedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        var publisher = new WiFiDirectAdvertisementPublisher();
        publisher.Advertisement.IsAutonomousGroupOwnerEnabled = true;
        publisher.Advertisement.LegacySettings.IsEnabled = true;
        publisher.Advertisement.LegacySettings.Ssid = network.Ssid;
        publisher.Advertisement.LegacySettings.Passphrase = new PasswordCredential { Password = network.Passphrase };
        publisher.StatusChanged += (_, e) =>
        {
            if (e.Status is WiFiDirectAdvertisementPublisherStatus.Started or WiFiDirectAdvertisementPublisherStatus.Aborted)
                started.TrySetResult(e);
        };


        try
        {
            publisher.Start();
            var status = await started.Task.WaitAsync(StartTimeout, cancellationToken);
            if (status.Status != WiFiDirectAdvertisementPublisherStatus.Started)
                throw new AccessPointUnavailableException(status.Error switch
                {
                    WiFiDirectError.RadioNotAvailable => WifiOffMessage,
                    WiFiDirectError.ResourceInUse => "Wi-Fi is busy with something else, such as Mobile hotspot. Turn that off, then try again.",
                    _ => "Windows couldn't create the Wi-Fi network",
                });

            var address = await WaitForAddress(cancellationToken)
                          ?? throw new AccessPointUnavailableException("The Wi-Fi network was created but never got an address");
            _publisher = publisher;
            return new AccessPointInfo(network, address);
        }
        catch (Exception e)
        {
            // whatever went wrong, including cancellation, nothing may keep broadcasting
            StopPublisher(publisher);
            if (e is AccessPointUnavailableException or OperationCanceledException) throw;
            // e.g. no Wi-Fi adapter, or a driver without Wi-Fi Direct support
            throw new AccessPointUnavailableException(e is TimeoutException
                ? "Windows didn't start the Wi-Fi network in time"
                : "This computer can't create a Wi-Fi network", e);
        }
    }

    private const string WifiOffMessage = "Wi-Fi is turned off on this computer. Turn it on, then try again.";

    private async Task<bool> IsWifiOff()
    {
        try
        {
            var radios = await Radio.GetRadiosAsync();
            var wifi = radios.Where(r => r.Kind == RadioKind.WiFi).ToList();
            return wifi.Count > 0 && wifi.All(r => r.State != RadioState.On);
        }
        catch (Exception e)
        {
            // can't tell; starting the network reports the problem instead
            logger.LogInformation(e, "Couldn't read the Wi-Fi radio's state");
            return false;
        }
    }

    private void StopPublisher(WiFiDirectAdvertisementPublisher publisher)
    {
        // stopping one that already stopped or aborted throws; one still starting (a timeout) must be stopped
        if (publisher.Status is WiFiDirectAdvertisementPublisherStatus.Aborted or WiFiDirectAdvertisementPublisherStatus.Stopped) return;
        try
        {
            publisher.Stop();
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Couldn't stop the local sync Wi-Fi network");
        }
    }

    public Task Stop()
    {
        if (_publisher is not null) StopPublisher(_publisher);
        _publisher = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Windows gives the group owner its address (192.168.137.1 in testing) on a "Microsoft Wi-Fi Direct Virtual
    /// Adapter" a second or two after the network starts. Until then the adapter has a link-local placeholder
    /// (169.254.x.x) that disappears, so it's skipped. Only that adapter counts: any other address would be a
    /// different network.
    /// </summary>
    private static async Task<IPAddress?> WaitForAddress(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            var address = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                              && nic.Description.StartsWith("Microsoft Wi-Fi Direct Virtual Adapter", StringComparison.OrdinalIgnoreCase))
                .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
                .Select(a => a.Address)
                .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !LocalAddresses.IsLinkLocal(a));
            if (address is not null) return address;
            await Task.Delay(250, cancellationToken);
        }
        return null;
    }
}
