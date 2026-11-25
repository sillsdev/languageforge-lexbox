using System.Text;

namespace FwLiteShared.LocalSync.Hosting;

/// <summary>A joiner as the hosting panel lists it.</summary>
public record PeerView(string RequestId, string DisplayName, string AppVersion, bool VersionMismatch, JoinState State, DateTimeOffset? LastSync);

/// <summary>A device on the host's network that hasn't asked to join.</summary>
/// <param name="Name">The user's name when the app is open on it; otherwise the name it gave the network, if any.</param>
/// <param name="HasApp">The app is open on it (it said hello), so it can join.</param>
public record NearbyDeviceView(string Address, string? Name, bool HasApp, bool VersionMismatch);

/// <param name="IsThisProject">False when the session is sharing another project than the one open in this window.</param>
/// <param name="NetworkName">The Wi-Fi network the host created; null when sharing over a network it was already on.</param>
/// <param name="WifiQr">The standard Wi-Fi QR text (<c>WIFI:T:WPA;S:...;P:...;;</c>): a phone's camera app joins the network from it.</param>
/// <param name="Invitation">The <c>fwlite-sync://join</c> invitation, for when the app can scan it.</param>
/// <param name="NearbyDevices">Devices on the network that haven't asked to join: ones with the app open first.</param>
public record HostingView(
    string ProjectName,
    bool IsThisProject,
    string Address,
    string? NetworkName,
    string? NetworkPassword,
    string? WifiQr,
    string Invitation,
    bool AutoApprove,
    PeerView[] Peers,
    NearbyDeviceView[] NearbyDevices)
{
    public static HostingView From(HostingInfo hosting, Guid currentProjectId)
    {
        var invitation = hosting.Invitation;
        var network = invitation.Network;
        return new HostingView(
            invitation.ProjectName,
            invitation.ProjectId == currentProjectId,
            invitation.Host.Authority,
            network?.Ssid,
            network?.Passphrase,
            network is null ? null : WifiQrText(network),
            invitation.ToUri(),
            hosting.Session.AutoApprove,
            [.. hosting.Session.Peers.Select(p => new PeerView(p.RequestId.ToString(), p.DisplayName, p.AppVersion, p.VersionMismatch, p.State, p.LastSync))],
            NearbyDevicesOf(hosting));
    }

    /// <summary>
    /// Merges the devices that said hello with the ones the network itself lists, leaving out those that asked to join.
    /// </summary>
    private static NearbyDeviceView[] NearbyDevicesOf(HostingInfo hosting)
    {
        var withApp = hosting.Session.NearbyDevices;
        var listed = withApp.Select(d => d.Address)
            .Concat(hosting.Session.Peers.Select(p => p.Address).OfType<string>())
            .ToHashSet();
        return
        [
            .. withApp.OrderBy(d => d.DisplayName).Select(d => new NearbyDeviceView(d.Address, d.DisplayName, HasApp: true, d.VersionMismatch)),
            .. hosting.NetworkDevices
                .Where(d => !listed.Contains(d.Address.ToString()))
                .OrderBy(d => d.Address.GetAddressBytes(), Comparer<byte[]>.Create((a, b) => ((ReadOnlySpan<byte>)a).SequenceCompareTo(b)))
                .Select(d => new NearbyDeviceView(d.Address.ToString(), d.Name, HasApp: false, VersionMismatch: false)),
        ];
    }

    /// <summary>The de facto standard Wi-Fi QR format, escaping its special characters with a backslash.</summary>
    public static string WifiQrText(AccessPointCredentials network) =>
        $"WIFI:T:WPA;S:{Escape(network.Ssid)};P:{Escape(network.Passphrase)};;";

    private static string Escape(string value)
    {
        var escaped = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c is '\\' or ';' or ',' or ':' or '"') escaped.Append('\\');
            escaped.Append(c);
        }
        return escaped.ToString();
    }
}

public record AvailableNetworkView(string Name, string Address);

/// <param name="CanCreateNetwork">This device can try to create its own Wi-Fi network.</param>
/// <param name="Networks">Networks it's already on, to share over instead.</param>
public record HostingOptions(bool CanCreateNetwork, AvailableNetworkView[] Networks);

public record StartHostingResult(HostingView? Hosting, HostingProblem? Problem);

public record NearbyHostView(string Address, string ProjectName, bool IsThisProject, bool VersionMismatch);
