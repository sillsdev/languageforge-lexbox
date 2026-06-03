using FwLiteShared.Auth;
using FwLiteShared.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Client;
using Moq;

namespace FwLiteShared.Tests.Auth;

// Covers the GetAuth catch/switch glue end-to-end; the classifier policy itself is in OAuthClientFailureClassifierTests.
public class OAuthClientSilentRefreshTests
{
    private static readonly LexboxServer Server = new(new Uri("https://example.test/"), "Test");

    private static (TestableOAuthClient client, Mock<IPublicClientApplication> appMock, GlobalEventBus eventBus, List<AuthenticationChangedEvent> events) BuildClient(
        Func<IAccount, bool, Task<AuthenticationResult>> acquire)
    {
        var account = Mock.Of<IAccount>(a => a.HomeAccountId == new AccountId("uid.tid", "uid", "tid"));
        var accounts = new List<IAccount> { account };
        var appMock = new Mock<IPublicClientApplication>(MockBehavior.Strict);
        //return a snapshot like the real MSAL client: Logout enumerates the result while RemoveAsync mutates the cache.
        appMock.Setup(a => a.GetAccountsAsync()).ReturnsAsync(() => accounts.ToList());
        appMock.Setup(a => a.RemoveAsync(It.IsAny<IAccount>()))
            .Returns<IAccount>(a => { accounts.Remove(a); return Task.CompletedTask; });

        var eventBus = new GlobalEventBus(NullLogger<GlobalEventBus>.Instance);
        var events = new List<AuthenticationChangedEvent>();
        eventBus.OnAuthenticationChanged.Subscribe(events.Add);

        var client = new TestableOAuthClient(appMock.Object, Server, eventBus, acquire);
        return (client, appMock, eventBus, events);
    }

    [Fact]
    public async Task TransientHttpError_KeepsCachedAccount()
    {
        var (client, appMock, _, events) = BuildClient((_, _) =>
            throw new MsalServiceException("service_error", "upstream returned 502"));

        var result = await client.GetAuth();

        result.Should().BeNull();
        (await appMock.Object.GetAccountsAsync()).Should().HaveCount(1);
        appMock.Verify(a => a.RemoveAsync(It.IsAny<IAccount>()), Times.Never);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task InvalidGrant_RemovesAccountAndRaisesEvent()
    {
        var (client, appMock, _, events) = BuildClient((_, _) =>
            throw new MsalUiRequiredException("invalid_grant", "refresh token expired"));

        var result = await client.GetAuth();

        result.Should().BeNull();
        (await appMock.Object.GetAccountsAsync()).Should().BeEmpty();
        appMock.Verify(a => a.RemoveAsync(It.IsAny<IAccount>()), Times.Once);
        events.Should().ContainSingle().Which.Server.Should().Be(Server);
    }

    [Fact]
    public async Task Cancellation_KeepsCachedAccount()
    {
        var (client, appMock, _, events) = BuildClient((_, _) =>
            throw new OperationCanceledException());

        var result = await client.GetAuth();

        result.Should().BeNull();
        (await appMock.Object.GetAccountsAsync()).Should().HaveCount(1);
        appMock.Verify(a => a.RemoveAsync(It.IsAny<IAccount>()), Times.Never);
        events.Should().BeEmpty();
    }

    // A transient failure must not hand callers the now-expired access token it was trying to replace,
    // otherwise CreateHttpClient sends a stale bearer and gets 401s. The account (refresh token) stays.
    [Fact]
    public async Task TransientFailure_AfterExpiry_ReturnsNullButKeepsAccount()
    {
        var calls = 0;
        var (client, appMock, _, events) = BuildClient((account, _) =>
        {
            calls++;
            if (calls == 1) return Task.FromResult(ExpiredResult(account));
            throw new MsalServiceException("service_error", "upstream returned 502");
        });

        (await client.GetAuth()).Should().NotBeNull("the first acquisition succeeds");
        var afterExpiry = await client.GetAuth(forceRefresh: true);

        afterExpiry.Should().BeNull("a transient refresh failure must not return the expired token");
        (await appMock.Object.GetAccountsAsync()).Should().HaveCount(1);
        appMock.Verify(a => a.RemoveAsync(It.IsAny<IAccount>()), Times.Never);
        events.Should().BeEmpty();
    }

    // Logout must take the same lock as GetAuth: an acquisition already in flight when logout starts
    // must not be able to repopulate _authResult and silently sign the user back in.
    [Fact]
    public async Task Logout_WaitsForInFlightAcquire_AndStaysLoggedOut()
    {
        var release = new TaskCompletionSource();
        var acquireEntered = new TaskCompletionSource();
        var (client, appMock, _, _) = BuildClient(async (account, _) =>
        {
            acquireEntered.TrySetResult();
            await release.Task;
            return ExpiredResult(account);
        });

        var getAuth = client.GetAuth().AsTask();
        await acquireEntered.Task;

        var logout = client.Logout();
        logout.IsCompleted.Should().BeFalse("logout must block on the in-flight acquisition's lock");

        release.SetResult();
        await Task.WhenAll(getAuth, logout);

        (await appMock.Object.GetAccountsAsync()).Should().BeEmpty();
        (await client.GetCurrentToken()).Should().BeNull("logout must win the race, not the in-flight acquire");
    }

    private static AuthenticationResult ExpiredResult(IAccount account) => new(
        accessToken: "expired-token",
        isExtendedLifeTimeToken: false,
        uniqueId: account.HomeAccountId.ObjectId,
        expiresOn: DateTimeOffset.UtcNow.AddMinutes(-1),
        extendedExpiresOn: DateTimeOffset.UtcNow.AddMinutes(-1),
        tenantId: "tid",
        account: account,
        idToken: null,
        scopes: OAuthClient.DefaultScopes,
        correlationId: Guid.NewGuid(),
        tokenType: "Bearer");

    private sealed class TestableOAuthClient(
        IPublicClientApplication application,
        LexboxServer server,
        GlobalEventBus eventBus,
        Func<IAccount, bool, Task<AuthenticationResult>> acquire)
        : OAuthClient(application, server, NullLogger<OAuthClient>.Instance, eventBus)
    {
        internal override Task<AuthenticationResult> AcquireTokenSilentAsync(IAccount account, bool forceRefresh)
            => acquire(account, forceRefresh);
    }
}
