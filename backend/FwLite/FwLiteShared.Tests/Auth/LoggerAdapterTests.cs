using FwLiteShared.Auth;

namespace FwLiteShared.Tests.Auth;

public class LoggerAdapterTests
{
    [Theory]
    [InlineData(
        "False MSAL 4.91.1.0 MSAL.NetCore .NET 10.0.14 Microsoft Windows 10.0.26200 [2026-11-10 09:06:26Z - 1a2b3c4d-0000-4000-8000-000000000001] ==== GetAccounts started - GetAccounts ====",
        "[1a2b3c4d] ==== GetAccounts started - GetAccounts ====")]
    [InlineData(
        "False MSAL 4.91.1.0 MSAL.NetCore .NET 10.0.15 Microsoft Windows 10.0.26200 [2026-11-16 07:35:07Z] [Internal cache] GetAllRefreshTokens (no partition) found 1 refresh tokens:",
        "[Internal cache] GetAllRefreshTokens (no partition) found 1 refresh tokens:")]
    [InlineData(
        "True MSAL 4.91.1.0 MSAL.Xamarin.Android .NET 10.0.15 34 [2026-11-16 07:35:07Z - 5e6f7a8b-0000-4000-8000-000000000002] ",
        "[5e6f7a8b] ")]
    [InlineData(
        "False MSAL 4.91.1.0 MSAL.NetCore .NET 10.0.15 Microsoft Windows 10.0.26200 [2026-11-16 07:35:07Z - 5e6f7a8b-0000-4000-8000-000000000002] (FwLite: v2026-11-16-3073dd1c) [Internal cache] Found (no partition) tokens",
        "[5e6f7a8b] [Internal cache] Found (no partition) tokens")]
    [InlineData(
        "False MSAL 4.91.1.0 MSAL.NetCore .NET 10.0.15 Microsoft Windows 10.0.26200 [2026-11-16 07:35:07Z] (FwLite) (not ours) message",
        "(not ours) message")]
    [InlineData("a message without the prefix [2026-11-16 07:35:07Z] stays", "a message without the prefix [2026-11-16 07:35:07Z] stays")]
    public void StripMsalPrefix_KeepsTheMessageAndCorrelationId(string message, string expected)
    {
        LoggerAdapter.StripMsalPrefix(message).Should().Be(expected);
    }
}
