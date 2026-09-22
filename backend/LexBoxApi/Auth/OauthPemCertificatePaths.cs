namespace LexBoxApi.Auth;

/// <summary>
/// Default mount paths for OAuth signing/encryption PEM pairs (k8s TLS secret layout).
/// Companion last-seen/previous slots are optional until the retainer CronJob creates them.
/// </summary>
public static class OauthPemCertificatePaths
{
    public const string CertFileName = "tls.crt";
    public const string KeyFileName = "tls.key";

    public static readonly string[] SigningDirectories =
    [
        "/oauth-certs/signing",
        "/oauth-certs/signing-last-seen",
        "/oauth-certs/signing-previous",
    ];

    public static readonly string[] EncryptionDirectories =
    [
        "/oauth-certs/encryption",
        "/oauth-certs/encryption-last-seen",
        "/oauth-certs/encryption-previous",
    ];
}
