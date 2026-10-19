namespace LexCore.Analytics;

/// <summary>
/// Builds the base set of Mixpanel event properties common to every product.
/// Callers add product-specific and identity properties (e.g. <c>$device_id</c>, <c>$user_id</c>).
/// </summary>
public static class MixpanelProperties
{
    public static Dictionary<string, object?> CreateBase(
        string token,
        string? product,
        DateTimeOffset time,
        string insertId)
    {
        var properties = new Dictionary<string, object?>
        {
            ["token"] = token,
            ["time"] = time.ToUnixTimeSeconds(),
            ["$insert_id"] = insertId,
        };
        if (!string.IsNullOrEmpty(product))
            properties["product"] = product;
        return properties;
    }
}
