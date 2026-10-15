using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using LexBoxApi.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;
using OpenIddict.Validation;

namespace Testing.LexBoxApi.Auth;

public class OauthPemCredentialHotReloadTests
{
    [Fact]
    public void CheckForChanges_SignalsReload_WhenPemContentChanges()
    {
        using var root = new TempPemRoot();
        var certA = CreateSelfSigned("CN=A");
        WritePemPair(root.Dir("signing"), certA);
        WritePemPair(root.Dir("encryption"), certA);

        using var source = new OauthPemOptionsChangeTokenSource(
            [root.Dir("signing")],
            [root.Dir("encryption")],
            pollingInterval: TimeSpan.FromHours(1));

        using var fired = new ManualResetEventSlim(false);
        IOptionsChangeTokenSource<OpenIddictServerOptions> serverSource = source;
        using var registration = ChangeToken.OnChange(serverSource.GetChangeToken, fired.Set);

        source.CheckForChanges().Should().BeFalse();
        fired.IsSet.Should().BeFalse();

        var certB = CreateSelfSigned("CN=B");
        WritePemPair(root.Dir("signing"), certB);

        source.CheckForChanges().Should().BeTrue();
        fired.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue();
    }

    [Fact]
    public async Task OptionsMonitor_ReloadsServerAndValidationCredentials_AfterPemSwap()
    {
        using var root = new TempPemRoot();
        var signingOld = CreateSelfSigned("CN=Sign-Old");
        var signingNew = CreateSelfSigned("CN=Sign-New");
        var encryptionOld = CreateSelfSigned("CN=Enc-Old");
        var encryptionNew = CreateSelfSigned("CN=Enc-New");

        WritePemPair(root.Dir("signing"), signingOld);
        WritePemPair(root.Dir("encryption"), encryptionOld);

        var signingDirs = new[]
        {
            root.Dir("signing"),
            root.Dir("signing-last-seen"),
            root.Dir("signing-previous"),
        };
        var encryptionDirs = new[]
        {
            root.Dir("encryption"),
            root.Dir("encryption-last-seen"),
            root.Dir("encryption-previous"),
        };

        var source = new OauthPemOptionsChangeTokenSource(signingDirs, encryptionDirs, TimeSpan.FromHours(1));
        await using var provider = BuildProvider(signingDirs, encryptionDirs, source);

        var serverMonitor = provider.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>();
        var validationMonitor = provider.GetRequiredService<IOptionsMonitor<OpenIddictValidationOptions>>();

        GetThumbprints(serverMonitor.CurrentValue.SigningCredentials).Should().Equal(signingOld.Thumbprint);
        GetThumbprints(serverMonitor.CurrentValue.EncryptionCredentials).Should().Equal(encryptionOld.Thumbprint);
        GetThumbprints(validationMonitor.CurrentValue.EncryptionCredentials).Should().Equal(encryptionOld.Thumbprint);
        validationMonitor.CurrentValue.Configuration!.SigningKeys.Should().ContainSingle();

        // Retain old key in previous slot; put new key in current (rollover mid-process).
        WritePemPair(root.Dir("signing-previous"), signingOld);
        WritePemPair(root.Dir("encryption-previous"), encryptionOld);
        WritePemPair(root.Dir("signing"), signingNew);
        WritePemPair(root.Dir("encryption"), encryptionNew);

        source.CheckForChanges().Should().BeTrue();

        GetThumbprints(serverMonitor.CurrentValue.SigningCredentials)
            .Should().BeEquivalentTo([signingOld.Thumbprint, signingNew.Thumbprint]);
        GetThumbprints(serverMonitor.CurrentValue.EncryptionCredentials)
            .Should().BeEquivalentTo([encryptionOld.Thumbprint, encryptionNew.Thumbprint]);

        // UseLocalServer-style import must re-run when validation options recreate.
        var validationEncryptionThumbprints = GetThumbprints(validationMonitor.CurrentValue.EncryptionCredentials).ToArray();
        validationEncryptionThumbprints.Should().BeEquivalentTo(
            [encryptionOld.Thumbprint, encryptionNew.Thumbprint],
            because: $"validation encryption thumbprints were [{string.Join(", ", validationEncryptionThumbprints)}]");
        validationMonitor.CurrentValue.Configuration!.SigningKeys.Should().HaveCount(2);
    }

    [Fact]
    public async Task RedeemAfterSwap_TokenProtectedWithOldKey_StillDecryptsWithoutRestart()
    {
        using var root = new TempPemRoot();
        var signingOld = CreateSelfSigned("CN=Sign-Old");
        var signingNew = CreateSelfSigned("CN=Sign-New");
        var encryptionOld = CreateSelfSigned("CN=Enc-Old");
        var encryptionNew = CreateSelfSigned("CN=Enc-New");

        WritePemPair(root.Dir("signing"), signingOld);
        WritePemPair(root.Dir("encryption"), encryptionOld);

        var signingDirs = new[]
        {
            root.Dir("signing"),
            root.Dir("signing-previous"),
        };
        var encryptionDirs = new[]
        {
            root.Dir("encryption"),
            root.Dir("encryption-previous"),
        };

        var source = new OauthPemOptionsChangeTokenSource(signingDirs, encryptionDirs, TimeSpan.FromHours(1));
        await using var provider = BuildProvider(signingDirs, encryptionDirs, source);

        var serverMonitor = provider.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>();
        var before = serverMonitor.CurrentValue;

        // Simulate an OpenIddict refresh token: nested JWT signed then encrypted with the then-current key.
        var handler = new JsonWebTokenHandler();
        var protectedToken = handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("sub", "lexbox-user"), new Claim("token_usage", "refresh_token")]),
            SigningCredentials = before.SigningCredentials[0],
            EncryptingCredentials = before.EncryptionCredentials[0],
        });

        WritePemPair(root.Dir("signing-previous"), signingOld);
        WritePemPair(root.Dir("encryption-previous"), encryptionOld);
        WritePemPair(root.Dir("signing"), signingNew);
        WritePemPair(root.Dir("encryption"), encryptionNew);
        source.CheckForChanges().Should().BeTrue();

        var after = serverMonitor.CurrentValue;
        GetThumbprints(after.SigningCredentials)
            .Should().BeEquivalentTo([signingOld.Thumbprint, signingNew.Thumbprint]);
        GetThumbprints(after.EncryptionCredentials)
            .Should().BeEquivalentTo([encryptionOld.Thumbprint, encryptionNew.Thumbprint]);

        // Prefer the new signing cert for issuance, but keep old keys for redemption.
        var preferredSigning = after.SigningCredentials
            .OrderByDescending(c => ((X509SecurityKey)c.Key).Certificate.NotAfter)
            .First();
        GetThumbprint(preferredSigning).Should().Be(signingNew.Thumbprint);

        var result = await handler.ValidateTokenAsync(protectedToken, new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            IssuerSigningKeys = after.SigningCredentials.Select(c => c.Key),
            TokenDecryptionKeys = after.EncryptionCredentials.Select(c => c.Key),
        });

        result.IsValid.Should().BeTrue(because: result.Exception?.ToString());
        result.ClaimsIdentity!.FindFirst("sub")!.Value.Should().Be("lexbox-user");
        result.ClaimsIdentity.FindFirst("token_usage")!.Value.Should().Be("refresh_token");
    }

    private static ServiceProvider BuildProvider(
        IReadOnlyList<string> signingDirs,
        IReadOnlyList<string> encryptionDirs,
        OauthPemOptionsChangeTokenSource source)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton(source);
        services.AddSingleton<IOptionsChangeTokenSource<OpenIddictServerOptions>>(source);
        services.AddSingleton<IOptionsChangeTokenSource<OpenIddictValidationOptions>>(source);
        services.AddSingleton<IConfigureOptions<OpenIddictServerOptions>>(
            new OauthPemOpenIddictServerConfigurer(signingDirs, encryptionDirs));

        // Mirror UseLocalServer(): import keys from current server options whenever validation options are created.
        services.AddSingleton<IConfigureOptions<OpenIddictValidationOptions>>(sp =>
            new ConfigureOptions<OpenIddictValidationOptions>(options =>
            {
                var server = sp.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>().CurrentValue;
                options.Configuration ??= new();
                foreach (var credentials in server.SigningCredentials)
                    options.Configuration.SigningKeys.Add(credentials.Key);
                foreach (var credentials in server.EncryptionCredentials)
                    options.EncryptionCredentials.Add(credentials);
            }));

        return services.BuildServiceProvider();
    }

    private static IEnumerable<string> GetThumbprints(IEnumerable<SigningCredentials> credentials) =>
        credentials.Select(GetThumbprint);

    private static IEnumerable<string> GetThumbprints(IEnumerable<EncryptingCredentials> credentials) =>
        credentials.Select(GetThumbprint);

    private static string GetThumbprint(SigningCredentials credentials) =>
        ((X509SecurityKey)credentials.Key).Certificate.Thumbprint;

    private static string GetThumbprint(EncryptingCredentials credentials) =>
        ((X509SecurityKey)credentials.Key).Certificate.Thumbprint;

    private static X509Certificate2 CreateSelfSigned(string subject)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        // Stagger NotAfter so "preferred" signing cert selection is deterministic in redeem-after-swap.
        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = subject.Contains("New", StringComparison.Ordinal)
            ? DateTimeOffset.UtcNow.AddYears(2)
            : DateTimeOffset.UtcNow.AddYears(1);
        return request.CreateSelfSigned(notBefore, notAfter);
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
        private readonly string _root = Path.Combine(Path.GetTempPath(), "lexbox-oauth-pem-reload-" + Guid.NewGuid().ToString("N"));

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
