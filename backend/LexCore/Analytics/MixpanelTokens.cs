using System.Text;

namespace LexCore.Analytics;

/// <summary>
/// Mixpanel project tokens shared by every product that sends to the unified Mixpanel project.
/// These are write-only ingestion tokens, not secrets (they're exposed in every client request),
/// but they're base64-encoded here so a plaintext token can't be trivially scraped from the public repo.
/// </summary>
public static class MixpanelTokens
{
    private const string DebugProjectTokenEncoded = "NWI5MDE3MjZjZDMzMGNmNmZhMWQyNzBmZTNjNzA1ZTg=";
    private const string ProductionProjectTokenEncoded = "YzA5ZDZhYmVjZWQ1MTE0YjBjM2YzMGY2ZjU1YmE3NjI=";

    /// <summary>Mixpanel debug/test project token. Decoded at runtime; not a secret.</summary>
    public static string DebugProjectToken { get; } = DecodeToken(DebugProjectTokenEncoded);

    /// <summary>Mixpanel release/production project token. Decoded at runtime; not a secret.</summary>
    public static string ProductionProjectToken { get; } = DecodeToken(ProductionProjectTokenEncoded);

    public static string DecodeToken(string encoded) => Encoding.UTF8.GetString(Convert.FromBase64String(encoded));

    /// <summary>
    /// Development uses the debug token; release uses the production token.
    /// A null/whitespace production token means "do not send".
    /// </summary>
    public static string? SelectToken(bool isDevelopment, string debugToken, string? productionToken)
    {
        var token = isDevelopment ? debugToken : productionToken;
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }
}
