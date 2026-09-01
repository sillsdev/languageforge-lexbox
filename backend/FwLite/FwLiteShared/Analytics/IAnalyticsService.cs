using Microsoft.JSInterop;

namespace FwLiteShared.Analytics;

public interface IAnalyticsService
{
    /// <summary>
    /// Whether analytics is currently enabled. Enabled by default; users may opt out.
    /// Exposed to the frontend via JSInterop.
    /// </summary>
    [JSInvokable]
    bool GetAnalyticsEnabled();

    /// <summary>
    /// Enable or disable analytics. Disabling stops all event sends; the opt-out is persisted.
    /// Exposed to the frontend via JSInterop.
    /// </summary>
    [JSInvokable]
    void SetAnalyticsEnabled(bool enabled);

    /// <summary>
    /// Queue an event for Mixpanel. Never throws; send failures are logged and swallowed.
    /// <paramref name="time"/> is the event occurrence; when omitted, the system clock is used.
    /// </summary>
    void Track(
        string eventName,
        IReadOnlyDictionary<string, object?>? properties = null,
        DateTimeOffset? time = null);

    /// <summary>Attach Mixpanel <c>$user_id</c> (persisted). A different user rotates <c>$device_id</c>. Empty values are ignored.</summary>
    void Identify(string userId);

    /// <summary>Clear persisted <c>$user_id</c> and rotate <c>$device_id</c> (logout on a shared device).</summary>
    void Reset();
}
