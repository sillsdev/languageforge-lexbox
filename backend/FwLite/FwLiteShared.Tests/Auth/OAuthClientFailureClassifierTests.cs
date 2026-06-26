using FwLiteShared.Auth;
using Microsoft.Identity.Client;

namespace FwLiteShared.Tests.Auth;

// Guards the policy that decides whether a failure inside AcquireTokenSilent logs the user out:
// historically any network blip triggered RemoveAsync, permanently wiping the cached refresh token.
public class OAuthClientFailureClassifierTests
{
    [Fact]
    public void MsalUiRequiredException_RemovesAccount()
    {
        var outcome = OAuthClient.ClassifySilentAuthFailure(
            new MsalUiRequiredException("error_code", "ui required"));

        outcome.Should().Be(OAuthClient.SilentAuthFailureOutcome.RemoveAccount);
    }

    [Fact]
    public void MultipleMatchingTokens_RemovesAccount()
    {
        var outcome = OAuthClient.ClassifySilentAuthFailure(
            new MsalClientException("multiple_matching_tokens_detected", "cache is inconsistent"));

        outcome.Should().Be(OAuthClient.SilentAuthFailureOutcome.RemoveAccount);
    }

    public static readonly TheoryData<Exception> TransientOrServerSideFailures =
    [
        //boundary: removal is keyed on the error code, not the MsalClientException type
        new MsalClientException("some_other_code", "unrelated client failure"),
        //boundary: only the UiRequired subclass of MsalServiceException is fatal
        new MsalServiceException("service_error", "upstream returned 502"),
        //HttpClient timeout; GetAuth passes no CancellationToken, so cancellation is always MSAL-internal
        new TaskCanceledException("http timeout"),
        new InvalidOperationException("something unexpected"),
    ];

    [Theory]
    [MemberData(nameof(TransientOrServerSideFailures))]
    public void AnyOtherFailure_KeepsCachedCredentials(Exception e)
    {
        var outcome = OAuthClient.ClassifySilentAuthFailure(e);

        outcome.Should().Be(OAuthClient.SilentAuthFailureOutcome.KeepCachedCredentials);
    }

    [Fact]
    public void OidcFetchFailure_IsOffline()
    {
        //the real-world offline error: MSAL fails to fetch the OIDC config, wrapping the connection failure
        var e = new MsalServiceException("oidc_failure", "Failed to retrieve OIDC configuration",
            new HttpRequestException("Connection failure"));

        var result = OAuthClient.ClassifyInteractiveLoginFailure(e);

        result.Should().Be(LoginResult.Offline);
    }

    [Fact]
    public void UserCancel_IsCancelled()
    {
        //a cancel is a cancel regardless of connectivity; offline-with-warm-cache also lands here
        var e = new MsalClientException(MsalError.AuthenticationCanceledError, "User canceled authentication");

        var result = OAuthClient.ClassifyInteractiveLoginFailure(e);

        result.Should().Be(LoginResult.Cancelled);
    }

    [Fact]
    public void UnexpectedFailure_IsNotClassified_SoCallerRethrows()
    {
        var result = OAuthClient.ClassifyInteractiveLoginFailure(
            new InvalidOperationException("something unexpected"));

        result.Should().BeNull();
    }
}
