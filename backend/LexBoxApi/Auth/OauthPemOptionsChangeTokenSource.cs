using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenIddict.Server;
using OpenIddict.Validation;

namespace LexBoxApi.Auth;

/// <summary>
/// Polls OAuth PEM mount paths for content changes and invalidates OpenIddict server
/// and validation options. Polling (content hash) is used instead of a naive
/// <see cref="FileSystemWatcher"/> because Kubernetes Secret volume updates swap
/// the <c>..data</c> symlink and often produce no usable watch events.
/// </summary>
public sealed class OauthPemOptionsChangeTokenSource :
    BackgroundService,
    IOptionsChangeTokenSource<OpenIddictServerOptions>,
    IOptionsChangeTokenSource<OpenIddictValidationOptions>
{
    public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromSeconds(30);

    private readonly IReadOnlyList<string> _directories;
    private readonly TimeSpan _pollingInterval;
    private readonly ILogger<OauthPemOptionsChangeTokenSource>? _logger;
    private CancellationTokenSource _serverCts = new();
    private CancellationTokenSource _validationCts = new();
    private string _lastFingerprint;

    public OauthPemOptionsChangeTokenSource(
        IReadOnlyList<string> signingDirectories,
        IReadOnlyList<string> encryptionDirectories,
        TimeSpan? pollingInterval = null,
        ILogger<OauthPemOptionsChangeTokenSource>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(signingDirectories);
        ArgumentNullException.ThrowIfNull(encryptionDirectories);

        _directories = signingDirectories.Concat(encryptionDirectories).ToArray();
        _pollingInterval = pollingInterval ?? DefaultPollingInterval;
        _logger = logger;
        _lastFingerprint = ComputeFingerprint();
    }

    public OauthPemOptionsChangeTokenSource(ILogger<OauthPemOptionsChangeTokenSource> logger)
        : this(
            OauthPemCertificatePaths.SigningDirectories,
            OauthPemCertificatePaths.EncryptionDirectories,
            DefaultPollingInterval,
            logger)
    {
    }

    string IOptionsChangeTokenSource<OpenIddictServerOptions>.Name => Options.DefaultName;
    string IOptionsChangeTokenSource<OpenIddictValidationOptions>.Name => Options.DefaultName;

    IChangeToken IOptionsChangeTokenSource<OpenIddictServerOptions>.GetChangeToken() =>
        new CancellationChangeToken(_serverCts.Token);

    IChangeToken IOptionsChangeTokenSource<OpenIddictValidationOptions>.GetChangeToken() =>
        new CancellationChangeToken(_validationCts.Token);

    /// <summary>
    /// Recomputes the PEM content fingerprint and, when it changed, signals options reload.
    /// Exposed for tests and for an immediate check outside the poll loop.
    /// </summary>
    /// <returns><see langword="true"/> if a reload was signaled.</returns>
    public bool CheckForChanges()
    {
        string fingerprint;
        try
        {
            fingerprint = ComputeFingerprint();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to fingerprint OAuth PEM mounts; will retry on next poll");
            return false;
        }

        if (string.Equals(fingerprint, _lastFingerprint, StringComparison.Ordinal))
            return false;

        _logger?.LogInformation("OAuth PEM mount content changed; reloading OpenIddict credentials");
        _lastFingerprint = fingerprint;
        Reload();
        return true;
    }

    /// <summary>
    /// Forces OpenIddict server and validation options to recreate on next access.
    /// Server is invalidated before validation so <c>UseLocalServer()</c> re-import sees fresh credentials.
    /// </summary>
    public void Reload()
    {
        var previousServer = Interlocked.Exchange(ref _serverCts, new CancellationTokenSource());
        previousServer.Cancel();

        var previousValidation = Interlocked.Exchange(ref _validationCts, new CancellationTokenSource());
        previousValidation.Cancel();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Establish baseline without signaling a reload on process start.
        _lastFingerprint = ComputeFingerprint();

        using var timer = new PeriodicTimer(_pollingInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            CheckForChanges();
        }
    }

    private string ComputeFingerprint()
    {
        var orderedPaths = _directories
            .Where(static d => !string.IsNullOrWhiteSpace(d))
            .SelectMany(static directory => new[]
            {
                System.IO.Path.Combine(directory, OauthPemCertificatePaths.CertFileName),
                System.IO.Path.Combine(directory, OauthPemCertificatePaths.KeyFileName),
            })
            .OrderBy(static p => p, StringComparer.Ordinal)
            .ToArray();

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var path in orderedPaths)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(path));
            hash.AppendData("\0"u8);

            if (!File.Exists(path))
            {
                hash.AppendData("missing"u8);
                continue;
            }

            // Read resolved file bytes so k8s ..data symlink swaps are visible.
            hash.AppendData(File.ReadAllBytes(path));
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
