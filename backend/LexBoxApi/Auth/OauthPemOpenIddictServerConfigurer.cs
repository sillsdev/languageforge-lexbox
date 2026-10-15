using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;

namespace LexBoxApi.Auth;

/// <summary>
/// Registers all distinct OAuth signing/encryption PEMs into OpenIddict server options.
/// Loads inside <see cref="Configure"/> so <see cref="OauthPemOptionsChangeTokenSource"/> can recreate options on PEM change.
/// </summary>
public sealed class OauthPemOpenIddictServerConfigurer : IConfigureOptions<OpenIddictServerOptions>
{
    private readonly IReadOnlyList<string> _signingDirectories;
    private readonly IReadOnlyList<string> _encryptionDirectories;

    public OauthPemOpenIddictServerConfigurer()
        : this(OauthPemCertificatePaths.SigningDirectories, OauthPemCertificatePaths.EncryptionDirectories)
    {
    }

    public OauthPemOpenIddictServerConfigurer(
        IReadOnlyList<string> signingDirectories,
        IReadOnlyList<string> encryptionDirectories)
    {
        _signingDirectories = signingDirectories;
        _encryptionDirectories = encryptionDirectories;
    }

    public void Configure(OpenIddictServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (var certificate in OauthPemCertificateLoader.LoadDistinctFromDirectories(_signingDirectories))
        {
            EnsurePrivateKey(certificate);
            options.SigningCredentials.Add(
                new SigningCredentials(new X509SecurityKey(certificate), SecurityAlgorithms.RsaSha256));
        }

        foreach (var certificate in OauthPemCertificateLoader.LoadDistinctFromDirectories(_encryptionDirectories))
        {
            EnsurePrivateKey(certificate);
            options.EncryptionCredentials.Add(new EncryptingCredentials(
                new X509SecurityKey(certificate),
                SecurityAlgorithms.RsaOAEP,
                SecurityAlgorithms.Aes256CbcHmacSha512));
        }
    }

    private static void EnsurePrivateKey(X509Certificate2 certificate)
    {
        if (!certificate.HasPrivateKey)
        {
            throw new InvalidOperationException(
                $"OAuth certificate '{certificate.Thumbprint}' is missing a private key.");
        }
    }
}
