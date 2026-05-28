using FwLiteShared.Auth;
using Microsoft.Identity.Client;

namespace FwLiteShared.Tests.Auth;

// Guards against regressing the policy that decides whether a failure inside AcquireTokenSilent
// should log the user out. Historically a network blip or unexpected exception would trigger
// _application.RemoveAsync(account), permanently wiping the refresh token from the MSAL cache
// and forcing the user to sign in again. See OAuthClient.ClassifySilentAuthFailure.
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

    [Fact]
    public void MsalClientException_OtherErrorCode_KeepsCache()
    {
        var outcome = OAuthClient.ClassifySilentAuthFailure(
            new MsalClientException("some_other_code", "unrelated client failure"));

        outcome.Should().Be(OAuthClient.SilentAuthFailureOutcome.KeepCachedCredentials);
    }

    [Fact]
    public void MsalServiceException_WithHttpRequestException_KeepsCache()
    {
        var outcome = OAuthClient.ClassifySilentAuthFailure(
            new MsalServiceException("service_error", "network failed", new HttpRequestException("dns lookup failed")));

        outcome.Should().Be(OAuthClient.SilentAuthFailureOutcome.KeepCachedCredentials);
    }

    [Fact]
    public void MsalServiceException_WithoutInnerException_KeepsCache()
    {
        // The author originally treated MsalServiceException as fatal when the inner was HttpRequestException;
        // we now treat ALL non-UI-required service exceptions as transient so flaky upstream responses
        // (5xx, 502 from a proxy, etc.) don't log the user out.
        var outcome = OAuthClient.ClassifySilentAuthFailure(
            new MsalServiceException("service_error", "upstream returned 502"));

        outcome.Should().Be(OAuthClient.SilentAuthFailureOutcome.KeepCachedCredentials);
    }

    [Fact]
    public void HttpRequestException_KeepsCache()
    {
        var outcome = OAuthClient.ClassifySilentAuthFailure(new HttpRequestException("connection reset"));

        outcome.Should().Be(OAuthClient.SilentAuthFailureOutcome.KeepCachedCredentials);
    }

    [Fact]
    public void TaskCanceledException_FromTimeout_Rethrows()
    {
        // TaskCanceledException derives from OperationCanceledException - HttpClient's default 100s timeout
        // surfaces as one of these. Previously this fell into catch(Exception) and wiped the account.
        var outcome = OAuthClient.ClassifySilentAuthFailure(new TaskCanceledException("http timeout"));

        outcome.Should().Be(OAuthClient.SilentAuthFailureOutcome.Rethrow);
    }

    [Fact]
    public void OperationCanceledException_Rethrows()
    {
        var outcome = OAuthClient.ClassifySilentAuthFailure(new OperationCanceledException());

        outcome.Should().Be(OAuthClient.SilentAuthFailureOutcome.Rethrow);
    }

    [Fact]
    public void IOException_FromMsalCacheFileLock_KeepsCache()
    {
        // MsalCacheHelper uses a cross-process file lock; contention can surface as IOException.
        // We don't want a transient file-lock conflict to log the user out.
        var outcome = OAuthClient.ClassifySilentAuthFailure(new IOException("file in use by another process"));

        outcome.Should().Be(OAuthClient.SilentAuthFailureOutcome.KeepCachedCredentials);
    }

    [Fact]
    public void UnknownException_KeepsCache()
    {
        var outcome = OAuthClient.ClassifySilentAuthFailure(new InvalidOperationException("something unexpected"));

        outcome.Should().Be(OAuthClient.SilentAuthFailureOutcome.KeepCachedCredentials);
    }
}
