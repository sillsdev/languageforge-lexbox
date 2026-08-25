namespace FwLiteShared.Analytics;

public interface IAnalyticsService
{
    /// <summary>Mixpanel super property <c>host</c>: <c>maui</c> or <c>web</c>.</summary>
    string? Host { get; set; }

    /// <summary>
    /// Queue an event for Mixpanel. Never throws; send failures are logged and swallowed.
    /// </summary>
    void Track(string eventName, IReadOnlyDictionary<string, object?>? properties = null);

    /// <summary>Attach Mixpanel <c>$user_id</c> to subsequent events. Empty values are ignored.</summary>
    void Identify(string userId);

    /// <summary>Clear <c>$user_id</c> and rotate <c>$device_id</c> (logout on a shared device).</summary>
    void Reset();
}
