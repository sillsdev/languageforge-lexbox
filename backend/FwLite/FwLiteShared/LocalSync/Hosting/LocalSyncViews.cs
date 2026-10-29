using System.Text;

namespace FwLiteShared.LocalSync.Hosting;

/// <summary>A joiner as the hosting panel lists it.</summary>
public record PeerView(string RequestId, string DisplayName, string AppVersion, bool VersionMismatch, JoinState State, DateTimeOffset? LastSync);

/// <param name="IsThisProject">False when the session is sharing another project than the one open in this window.</param>
/// <param name="NetworkName">The Wi-Fi network the host created; null when sharing over a network it was already on.</param>
/// <param name="WifiQr">The standard Wi-Fi QR text (<c>WIFI:T:WPA;S:...;P:...;;</c>): a phone's camera app joins the network from it.</param>
/// <param name="Invitation">The <c>fwlite-sync://join</c> invitation, for when the app can scan it.</param>
public record HostingView(
    string ProjectName,
    bool IsThisProject,
    string Address,
    string? NetworkName,
    string? NetworkPassword,
    string? WifiQr,
    string Invitation,
    bool AutoApprove,
    PeerView[] Peers)
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
            [.. hosting.Session.Peers.Select(p => new PeerView(p.RequestId.ToString(), p.DisplayName, p.AppVersion, p.VersionMismatch, p.State, p.LastSync))]);
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

/// <param name="Error">Why hosting couldn't start (e.g. the Wi-Fi network couldn't be created), shown to the user.</param>
public record StartHostingResult(HostingView? Hosting, string? Error);

public record NearbyHostView(string Address, string ProjectName, bool IsThisProject, bool VersionMismatch);
