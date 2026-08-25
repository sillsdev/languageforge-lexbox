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
    IHostEnvironment environment,
    IPreferencesService preferences,
    ILogger<AnalyticsService> logger) : IAnalyticsService
{
    private readonly Lock _identityLock = new();
    private string? _deviceId;
    private string? _userId;

    public string? Host { get; set; }

    public void Track(string eventName, IReadOnlyDictionary<string, object?>? properties = null)
    {
        _ = TrackAsync(eventName, properties);
    }

    public void Identify(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;
        lock (_identityLock)
        {
            _userId = userId;
        }
    }

    public void Reset()
    {
        var next = Guid.NewGuid().ToString();
        lock (_identityLock)
        {
            _userId = null;
            _deviceId = next;
            preferences.Set(nameof(PreferenceKey.AnalyticsDeviceId), next);
        }
    }

    internal string GetOrCreateDeviceId()
    {
        lock (_identityLock)
        {
            if (!string.IsNullOrEmpty(_deviceId))
                return _deviceId;
            var stored = preferences.Get(nameof(PreferenceKey.AnalyticsDeviceId));
            if (!string.IsNullOrEmpty(stored))
            {
                _deviceId = stored;
                return stored;
            }

            var created = Guid.NewGuid().ToString();
            preferences.Set(nameof(PreferenceKey.AnalyticsDeviceId), created);
            _deviceId = created;
            return created;
        }
    }

    internal string? CurrentUserId
    {
        get
        {
            lock (_identityLock)
            {
                return _userId;
            }
        }
    }

    internal async Task TrackAsync(string eventName, IReadOnlyDictionary<string, object?>? properties = null)
    {
        try
        {
            var fwLite = config.Value;
            var token = MixpanelAnalytics.SelectToken(environment.IsDevelopment(), fwLite.UseDevAssets);
            if (token is null)
                return;

            var payload = new[]
            {
                new MixpanelTrackEvent(eventName,
                    BuildProperties(token, fwLite, Host, GetOrCreateDeviceId(), CurrentUserId, properties))
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

    private sealed record MixpanelTrackEvent(
        [property: JsonPropertyName("event")] string Event,
        Dictionary<string, object?> Properties);
}
