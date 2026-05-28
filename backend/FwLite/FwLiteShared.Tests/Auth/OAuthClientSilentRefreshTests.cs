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
        appMock.Setup(a => a.GetAccountsAsync()).ReturnsAsync(accounts);
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
    public async Task Cancellation_DoesNotRemoveAccount()
    {
        var (client, appMock, _, events) = BuildClient((_, _) =>
            throw new OperationCanceledException());

        var act = async () => await client.GetAuth();

        await act.Should().ThrowAsync<OperationCanceledException>();
        (await appMock.Object.GetAccountsAsync()).Should().HaveCount(1);
        appMock.Verify(a => a.RemoveAsync(It.IsAny<IAccount>()), Times.Never);
        events.Should().BeEmpty();
    }

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
