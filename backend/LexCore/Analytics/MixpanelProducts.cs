namespace LexCore.Analytics;

/// <summary>
/// Value of the Mixpanel <c>product</c> super-property, used to distinguish which product an event
/// came from in the shared Mixpanel project.
/// </summary>
public static class MixpanelProducts
{
    public const string FwLite = "fw-lite";
    public const string Lexbox = "lexbox";
}
