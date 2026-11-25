using System.Net;
using System.Security.Cryptography;
using System.Web;

namespace FwLiteShared.LocalSync.Hosting;

/// <summary>The Wi-Fi network a host created for its session.</summary>
public record AccessPointCredentials(string Ssid, string Passphrase)
{
    // no look-alikes (0/O, 1/I/l), so it can be typed from the hosting panel when there's no camera
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>A new network for each session, so nobody can rejoin one from an earlier session.</summary>
    public static AccessPointCredentials Generate() => new("FWLite-" + Random(4), Random(12));

    private static string Random(int length) => RandomNumberGenerator.GetString(Alphabet, length);
}

/// <summary>
/// What the hosting panel's QR code carries: everything a joiner needs to reach the host, so scanning it can connect
/// to the host's network and go straight to the host without searching.
/// </summary>
/// <param name="Host">The host's sync address, e.g. <c>http://192.168.137.1:5139</c>.</param>
/// <param name="Network">The network the host created; null when the host is on a network the joiner must already be on.</param>
public record JoinInvitation(Uri Host, Guid ProjectId, string ProjectName, AccessPointCredentials? Network)
{
    public const string Scheme = "fwlite-sync";

    public string ToUri()
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["host"] = Host.GetLeftPart(UriPartial.Authority);
        query["project"] = ProjectId.ToString();
        query["name"] = ProjectName;
        if (Network is not null)
        {
            query["ssid"] = Network.Ssid;
            query["pass"] = Network.Passphrase;
        }
        return $"{Scheme}://join?{query}";
    }

    /// <summary>Reads a scanned code. Returns null for anything that isn't an invitation to a host on the local network.</summary>
    public static JoinInvitation? TryParse(string text)
    {
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.Scheme != Scheme || uri.Host != "join") return null;
        var query = HttpUtility.ParseQueryString(uri.Query);
        if (!Uri.TryCreate(query["host"], UriKind.Absolute, out var host)
            || host.Scheme != Uri.UriSchemeHttp
            || !IPAddress.TryParse(host.Host, out var address)
            || !LocalAddresses.IsLocal(address)
            || !Guid.TryParse(query["project"], out var projectId))
            return null;

        var network = query["ssid"] is { Length: > 0 } ssid && query["pass"] is { Length: > 0 } pass
            ? new AccessPointCredentials(ssid, pass)
            : null;
        return new JoinInvitation(host, projectId, query["name"] ?? "", network);
    }
}
