using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FwLiteShared.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteShared.Analytics;

public class AnalyticsService(
    IHttpClientFactory httpClientFactory,
    IOptions<FwLiteConfig> config,
    IOptions<AnalyticsConfig> analyticsConfig,
    IHostEnvironment environment,
    IPreferencesService preferences,
    ILogger<AnalyticsService> logger) : IAnalyticsService
{
    private readonly Lock _identityLock = new();
    private bool _identityLoaded;
    private string? _deviceId;
    private string? _userId;

    public void Track(string eventName, IReadOnlyDictionary<string, object?>? properties = null)
    {
        _ = Task.Run(() => TrackAsync(eventName, properties));
    }

    public void Identify(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;
        lock (_identityLock)
        {
            EnsureIdentityLoadedLocked();
            if (!string.IsNullOrEmpty(_userId) && _userId != userId)
                ResetLocked();
            _userId = userId;
            preferences.Set(nameof(PreferenceKey.AnalyticsUserId), userId);
        }
    }

    public void Reset()
    {
        lock (_identityLock)
        {
            EnsureIdentityLoadedLocked();
            ResetLocked();
        }
    }

    internal string GetOrCreateDeviceId()
    {
        lock (_identityLock)
        {
            EnsureIdentityLoadedLocked();
            return _deviceId!;
        }
    }

    internal string? CurrentUserId
    {
        get
        {
            lock (_identityLock)
            {
                EnsureIdentityLoadedLocked();
                return _userId;
            }
        }
    }

    internal async Task TrackAsync(string eventName, IReadOnlyDictionary<string, object?>? properties = null)
    {
        try
        {
            var fwLite = config.Value;
            var analytics = analyticsConfig.Value;
            var token = MixpanelAnalytics.SelectToken(environment.IsDevelopment(), analytics);
            if (token is null)
                return;

            var payload = new[]
            {
                new MixpanelTrackEvent(eventName,
                    BuildProperties(token, fwLite, analytics.Host, GetOrCreateDeviceId(), CurrentUserId, properties))
            };

            var client = httpClientFactory.CreateClient(MixpanelAnalytics.HttpClientName);
            using var response = await client.PostAsJsonAsync(MixpanelAnalytics.TrackUrl, payload);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Mixpanel track returned {Status} for {Event}",
                    (int)response.StatusCode,
                    eventName);
            }
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to send analytics event {Event}", eventName);
        }
    }

    internal static Dictionary<string, object?> BuildProperties(
        string token,
        FwLiteConfig fwLite,
        string? host,
        string deviceId,
        string? userId,
        IReadOnlyDictionary<string, object?>? extra)
    {
        var properties = new Dictionary<string, object?>
        {
            ["token"] = token,
            ["$device_id"] = deviceId,
            ["app_version"] = fwLite.AppVersion,
            ["os"] = fwLite.Os.ToString(),
            ["edition"] = fwLite.Edition.ToString(),
        };
        if (!string.IsNullOrEmpty(userId))
            properties["$user_id"] = userId;
        if (!string.IsNullOrEmpty(host))
            properties["host"] = host;
        if (extra is null)
            return properties;
        foreach (var (key, value) in extra)
            properties[key] = value;
        return properties;
    }

    private void EnsureIdentityLoadedLocked()
    {
        if (_identityLoaded)
            return;

        var storedDevice = preferences.Get(nameof(PreferenceKey.AnalyticsDeviceId));
        if (string.IsNullOrEmpty(storedDevice))
        {
            storedDevice = Guid.NewGuid().ToString();
            preferences.Set(nameof(PreferenceKey.AnalyticsDeviceId), storedDevice);
        }

        _deviceId = storedDevice;
        _userId = preferences.Get(nameof(PreferenceKey.AnalyticsUserId));
        if (string.IsNullOrWhiteSpace(_userId))
            _userId = null;
        _identityLoaded = true;
    }

    private void ResetLocked()
    {
        _identityLoaded = true;
        _userId = null;
        preferences.Remove(nameof(PreferenceKey.AnalyticsUserId));
        var next = Guid.NewGuid().ToString();
        _deviceId = next;
        preferences.Set(nameof(PreferenceKey.AnalyticsDeviceId), next);
    }

    private sealed record MixpanelTrackEvent(
        [property: JsonPropertyName("event")] string Event,
        Dictionary<string, object?> Properties);
}
