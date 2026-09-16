using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using FwLiteShared.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace FwLiteShared.Analytics;

public class AnalyticsService(
    IHttpClientFactory httpClientFactory,
    IOptions<FwLiteConfig> config,
    IOptions<AnalyticsConfig> analyticsConfig,
    IHostEnvironment environment,
    IPreferencesService preferences,
    ILogger<AnalyticsService> logger,
    IEnumerable<IAnalyticsEventEnricher>? enrichers = null,
    TimeProvider? timeProvider = null) : IAnalyticsService
{
    private readonly Lock _identityLock = new();
    private bool _identityLoaded;
    private string? _deviceId;
    private string? _userId;
    private readonly IAnalyticsEventEnricher[] _enrichers = enrichers?.ToArray() ?? [];
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;

    [JSInvokable]
    public bool GetAnalyticsEnabled()
    {
        if (!analyticsConfig.Value.Enabled)
            return false;
        return preferences.Get(nameof(PreferenceKey.AnalyticsOptOut)) != "true";
    }

    [JSInvokable]
    public void SetAnalyticsEnabled(bool enabled)
    {
        if (enabled)
            preferences.Remove(nameof(PreferenceKey.AnalyticsOptOut));
        else
            preferences.Set(nameof(PreferenceKey.AnalyticsOptOut), "true");
    }

    public void Track(
        string eventName,
        IReadOnlyDictionary<string, object?>? properties = null,
        DateTimeOffset? time = null)
    {
        if (!GetAnalyticsEnabled())
            return;
        var occurredAt = time ?? _clock.GetUtcNow();
        var insertId = Guid.NewGuid().ToString();
        _ = Task.Run(() => TrackAsync(eventName, properties, occurredAt, insertId));
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

    /// <summary>
    /// Reads the device and user ids together under one lock so a concurrent <see cref="Identify"/>
    /// (which rotates the device id on a user switch) can't tear a track event into a mismatched pair.
    /// </summary>
    internal (string DeviceId, string? UserId) GetIdentitySnapshot()
    {
        lock (_identityLock)
        {
            EnsureIdentityLoadedLocked();
            return (_deviceId!, _userId);
        }
    }

    internal async Task TrackAsync(
        string eventName,
        IReadOnlyDictionary<string, object?>? properties = null,
        DateTimeOffset? time = null,
        string? insertId = null)
    {
        try
        {
            if (!GetAnalyticsEnabled())
                return;
            var fwLite = config.Value;
            var analytics = analyticsConfig.Value;
            var token = MixpanelAnalytics.SelectToken(environment.IsDevelopment(), analytics);
            if (token is null)
                return;

            var (deviceId, userId) = GetIdentitySnapshot();
            var eventProperties = BuildProperties(
                token,
                fwLite,
                analytics.Host,
                deviceId,
                userId,
                time ?? _clock.GetUtcNow(),
                insertId ?? Guid.NewGuid().ToString());
            foreach (var enricher in _enrichers)
                enricher.Enrich(eventProperties);
            Merge(eventProperties, properties);
            var payload = new[]
            {
                new MixpanelTrackEvent(eventName, eventProperties)
            };

            var client = httpClientFactory.CreateClient(MixpanelAnalytics.HttpClientName);
            using var response = await client.PostAsJsonAsync(MixpanelAnalytics.TrackUrl + "?ip=1", payload);
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
        DateTimeOffset time,
        string insertId)
    {
        var properties = new Dictionary<string, object?>
        {
            ["token"] = token,
            ["$device_id"] = deviceId,
            ["$app_version_string"] = fwLite.AppVersion,
            ["$os"] = fwLite.Os.ToString(),
            ["$os_version"] = RuntimeInformation.OSDescription,
            ["edition"] = fwLite.Edition.ToString(),
            ["time"] = time.ToUnixTimeSeconds(),
            ["$insert_id"] = insertId,
        };
        if (!string.IsNullOrEmpty(userId))
            properties["$user_id"] = userId;
        if (!string.IsNullOrEmpty(host))
            properties["host"] = host;
        return properties;
    }

    private static void Merge(Dictionary<string, object?> target, IReadOnlyDictionary<string, object?>? source)
    {
        if (source is null)
            return;
        foreach (var (key, value) in source)
            target[key] = value;
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
