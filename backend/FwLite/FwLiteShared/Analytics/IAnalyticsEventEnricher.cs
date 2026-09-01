namespace FwLiteShared.Analytics;

/// <summary>
/// Host-specific Mixpanel properties written into every tracked event.
/// </summary>
public interface IAnalyticsEventEnricher
{
    void Enrich(Dictionary<string, object?> properties);
}
