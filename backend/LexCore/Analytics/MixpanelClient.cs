using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace LexCore.Analytics;

/// <summary>
/// Transport that POSTs a single event to Mixpanel's <c>/track</c> endpoint.
/// Shared by every product. Never throws: send failures are logged and swallowed so analytics
/// can never break a caller's request path.
/// </summary>
public class MixpanelClient(IHttpClientFactory httpClientFactory, ILogger<MixpanelClient> logger)
{
    public const string TrackUrl = "https://api.mixpanel.com/track";
    public const string HttpClientName = "Mixpanel";

    /// <summary>
    /// Send one event. <paramref name="properties"/> must already contain the Mixpanel <c>token</c>.
    /// </summary>
    /// <param name="geolocateFromRequestIp">
    /// When true, append <c>?ip=1</c> so Mixpanel geolocates from the HTTP request's source IP. Correct for
    /// client-side senders (e.g. FwLite) where the request originates on the user's device. Leave false for
    /// server-side senders (e.g. LexBox), which must instead supply the end user's IP as the reserved
    /// <c>ip</c> property — otherwise every event would geolocate to the server.
    /// </param>
    public async Task SendAsync(
        string eventName,
        IReadOnlyDictionary<string, object?> properties,
        bool geolocateFromRequestIp = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new[] { new MixpanelTrackEvent(eventName, properties) };
            var client = httpClientFactory.CreateClient(HttpClientName);
            var url = geolocateFromRequestIp ? TrackUrl + "?ip=1" : TrackUrl;
            using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
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

    private sealed record MixpanelTrackEvent(
        [property: JsonPropertyName("event")] string Event,
        IReadOnlyDictionary<string, object?> Properties);
}
