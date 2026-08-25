using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteShared.Analytics;

public class AnalyticsService(
    IHttpClientFactory httpClientFactory,
    IOptions<FwLiteConfig> config,
    IHostEnvironment environment,
    ILogger<AnalyticsService> logger) : IAnalyticsService
{
    public string? Host { get; set; }

    public void Track(string eventName, IReadOnlyDictionary<string, object?>? properties = null)
    {
        _ = TrackAsync(eventName, properties);
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
                new MixpanelTrackEvent(eventName, BuildProperties(token, fwLite, Host, properties))
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
        IReadOnlyDictionary<string, object?>? extra)
    {
        var properties = new Dictionary<string, object?>
        {
            ["token"] = token,
            ["app_version"] = fwLite.AppVersion,
            ["os"] = fwLite.Os.ToString(),
            ["edition"] = fwLite.Edition.ToString(),
        };
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
