namespace FwLiteShared.Analytics;

public interface IAnalyticsService
{
    /// <summary>Mixpanel super property <c>host</c>: <c>maui</c> or <c>web</c>.</summary>
    string? Host { get; set; }

    /// <summary>
    /// Queue an event for Mixpanel. Never throws; send failures are logged and swallowed.
    /// </summary>
    void Track(string eventName, IReadOnlyDictionary<string, object?>? properties = null);
}
