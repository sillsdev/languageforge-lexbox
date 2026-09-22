using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using LexBoxApi.Auth;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;

namespace Testing.LexBoxApi.Auth;

public class OauthPemCertificateLoaderTests
{
    [Fact]
    public void LoadDistinctFromDirectories_LoadsAllPresentSlots_AndSkipsMissing()
    {
        using var root = new TempPemRoot();
        var certA = CreateSelfSigned("CN=A");
        var certB = CreateSelfSigned("CN=B");

        WritePemPair(root.Dir("signing"), certA);
        WritePemPair(root.Dir("signing-previous"), certB);
        // signing-last-seen intentionally absent

        var loaded = OauthPemCertificateLoader.LoadDistinctFromDirectories(
        [
            root.Dir("signing"),
            root.Dir("signing-last-seen"),
            root.Dir("signing-previous"),
        ]);

        loaded.Should().HaveCount(2);
        loaded.Select(c => c.Thumbprint).Should().BeEquivalentTo([certA.Thumbprint, certB.Thumbprint]);
    }

    [Fact]
    public void LoadDistinctFromDirectories_CurrentOnly_MatchesTodayLayout()
    {
        using var root = new TempPemRoot();
        var current = CreateSelfSigned("CN=Current");
        WritePemPair(root.Dir("signing"), current);

        var loaded = OauthPemCertificateLoader.LoadDistinctFromDirectories(
        [
            root.Dir("signing"),
            root.Dir("signing-last-seen"),
            root.Dir("signing-previous"),
        ]);

        loaded.Should().ContainSingle()
            .Which.Thumbprint.Should().Be(current.Thumbprint);
    }

    [Fact]
    public void LoadDistinctFromDirectories_DeduplicatesByThumbprint()
    {
        using var root = new TempPemRoot();
        var shared = CreateSelfSigned("CN=Shared");
        WritePemPair(root.Dir("signing"), shared);
        WritePemPair(root.Dir("signing-last-seen"), shared);
        WritePemPair(root.Dir("signing-previous"), shared);

        var loaded = OauthPemCertificateLoader.LoadDistinctFromDirectories(
        [
            root.Dir("signing"),
            root.Dir("signing-last-seen"),
            root.Dir("signing-previous"),
        ]);

        loaded.Should().ContainSingle()
            .Which.Thumbprint.Should().Be(shared.Thumbprint);
    }

    [Fact]
    public void Configurer_RegistersDistinctSigningAndEncryptionCredentials()
    {
        using var root = new TempPemRoot();
        var signingCurrent = CreateSelfSigned("CN=Sign-Current");
        var signingPrevious = CreateSelfSigned("CN=Sign-Previous");
        var encryptionCurrent = CreateSelfSigned("CN=Enc-Current");
        var encryptionLastSeen = CreateSelfSigned("CN=Enc-LastSeen");

        WritePemPair(root.Dir("signing"), signingCurrent);
        WritePemPair(root.Dir("signing-previous"), signingPrevious);
        WritePemPair(root.Dir("encryption"), encryptionCurrent);
        WritePemPair(root.Dir("encryption-last-seen"), encryptionLastSeen);

        var configurer = new OauthPemOpenIddictServerConfigurer(
            [
                root.Dir("signing"),
                root.Dir("signing-last-seen"),
                root.Dir("signing-previous"),
            ],
            [
                root.Dir("encryption"),
                root.Dir("encryption-last-seen"),
                root.Dir("encryption-previous"),
            ]);

        var options = new OpenIddictServerOptions();
        configurer.Configure(options);

        options.SigningCredentials.Should().HaveCount(2);
        options.SigningCredentials.Select(GetThumbprint).Should()
            .BeEquivalentTo([signingCurrent.Thumbprint, signingPrevious.Thumbprint]);

        options.EncryptionCredentials.Should().HaveCount(2);
        options.EncryptionCredentials.Select(GetThumbprint).Should()
            .BeEquivalentTo([encryptionCurrent.Thumbprint, encryptionLastSeen.Thumbprint]);
    }

    [Fact]
    public void Configurer_CurrentOnly_RegistersSingleSigningAndEncryption()
    {
        using var root = new TempPemRoot();
        var signing = CreateSelfSigned("CN=Sign");
        var encryption = CreateSelfSigned("CN=Enc");
        WritePemPair(root.Dir("signing"), signing);
        WritePemPair(root.Dir("encryption"), encryption);

        var configurer = new OauthPemOpenIddictServerConfigurer(
            [
                root.Dir("signing"),
                root.Dir("signing-last-seen"),
                root.Dir("signing-previous"),
            ],
            [
                root.Dir("encryption"),
                root.Dir("encryption-last-seen"),
                root.Dir("encryption-previous"),
            ]);

        var options = new OpenIddictServerOptions();
        configurer.Configure(options);

        options.SigningCredentials.Should().ContainSingle();
        GetThumbprint(options.SigningCredentials[0]).Should().Be(signing.Thumbprint);
        options.EncryptionCredentials.Should().ContainSingle();
        GetThumbprint(options.EncryptionCredentials[0]).Should().Be(encryption.Thumbprint);
    }

    private static string GetThumbprint(SigningCredentials credentials) =>
        ((X509SecurityKey)credentials.Key).Certificate.Thumbprint;

    private static string GetThumbprint(EncryptingCredentials credentials) =>
        ((X509SecurityKey)credentials.Key).Certificate.Thumbprint;

    private static X509Certificate2 CreateSelfSigned(string subject)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
    }

    private static void WritePemPair(string directory, X509Certificate2 certificate)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, OauthPemCertificatePaths.CertFileName), certificate.ExportCertificatePem());
        var key = certificate.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("Test certificate is missing an RSA private key.");
        File.WriteAllText(Path.Combine(directory, OauthPemCertificatePaths.KeyFileName), key.ExportPkcs8PrivateKeyPem());
    }

    private sealed class TempPemRoot : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "lexbox-oauth-pem-tests-" + Guid.NewGuid().ToString("N"));

        public TempPemRoot() => Directory.CreateDirectory(_root);

        public string Dir(string name) => Path.Combine(_root, name);

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_root))
                    Directory.Delete(_root, recursive: true);
            }
            catch
            {
                // Best-effort cleanup for temp test dirs.
            }
        }
    }
}
