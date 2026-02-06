using FwLiteShared;
using FwLiteShared.AppUpdate;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteMaui;

/// <summary>
/// Base class for MAUI platform update services. Provides shared functionality:
/// - LastUpdateCheck persistence via IPreferences
/// - Inherits HTTP update check from CorePlatformUpdateService
///
/// Windows and Android extend this class with platform-specific implementations.
/// </summary>
public abstract class MauiPlatformUpdateServiceBase : CorePlatformUpdateService
{
    private readonly IPreferences _preferences;
    private const string LastUpdateCheckKey = "lastUpdateChecked";

    protected MauiPlatformUpdateServiceBase(
        IHttpClientFactory httpClientFactory,
        IOptions<FwLiteConfig> config,
        ILogger<CorePlatformUpdateService> logger,
        IPreferences preferences)
        : base(httpClientFactory, config, logger)
    {
        _preferences = preferences;
    }

    public override DateTime LastUpdateCheck
    {
        get => _preferences.Get(LastUpdateCheckKey, DateTime.MinValue);
        set => _preferences.Set(LastUpdateCheckKey, value);
    }

    /// <summary>
    /// Platform-specific metered connection detection. MAUI's IConnectivity doesn't expose this.
    /// </summary>
    public abstract override bool IsOnMeteredConnection();
}
