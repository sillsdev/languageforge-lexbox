using System.Security.Cryptography;

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

/// <summary>Everything a joiner needs to reach the host.</summary>
/// <param name="Host">The host's sync address, e.g. <c>http://192.168.137.1:5139</c>.</param>
/// <param name="Network">The network the host created; null when the host is on a network the joiner must already be on.</param>
public record JoinInvitation(Uri Host, Guid ProjectId, string ProjectName, AccessPointCredentials? Network);
