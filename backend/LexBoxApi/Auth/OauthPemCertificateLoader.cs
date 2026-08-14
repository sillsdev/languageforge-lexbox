using System.Security.Cryptography.X509Certificates;

namespace LexBoxApi.Auth;

/// <summary>
/// Loads distinct X.509 certificates from PEM directory slots (<c>tls.crt</c> + <c>tls.key</c>).
/// Missing directories or companion files are skipped so a single current mount still works.
/// </summary>
public static class OauthPemCertificateLoader
{
    public static IReadOnlyList<X509Certificate2> LoadDistinctFromDirectories(IEnumerable<string> directories)
    {
        ArgumentNullException.ThrowIfNull(directories);

        var certificates = new List<X509Certificate2>();
        var seenThumbprints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in directories)
        {
            if (string.IsNullOrWhiteSpace(directory))
                continue;

            var certPath = System.IO.Path.Combine(directory, OauthPemCertificatePaths.CertFileName);
            var keyPath = System.IO.Path.Combine(directory, OauthPemCertificatePaths.KeyFileName);
            if (!File.Exists(certPath) || !File.Exists(keyPath))
                continue;

            var certificate = X509Certificate2.CreateFromPemFile(certPath, keyPath);
            if (!seenThumbprints.Add(certificate.Thumbprint))
            {
                certificate.Dispose();
                continue;
            }

            certificates.Add(certificate);
        }

        return certificates;
    }
}
