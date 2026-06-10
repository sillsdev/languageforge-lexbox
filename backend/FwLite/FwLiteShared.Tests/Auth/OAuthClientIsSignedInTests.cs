using FwLiteShared.Auth;
using FwLiteShared.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Client;
using Moq;

namespace FwLiteShared.Tests.Auth;

// IsSignedIn must be a purely local cache read: it answers "is there an account" without ever
// acquiring a token, so callers can tell "offline" apart from "not logged in".
public class OAuthClientIsSignedInTests
{
    private static readonly LexboxServer Server = new(new Uri("https://example.test/"), "Test");

    private static (OAuthClient client, Mock<IPublicClientApplication> appMock) BuildClient(IReadOnlyList<IAccount> accounts)
    {
        var appMock = new Mock<IPublicClientApplication>(MockBehavior.Strict);
        appMock.Setup(a => a.GetAccountsAsync()).ReturnsAsync(accounts);

        var eventBus = new GlobalEventBus(NullLogger<GlobalEventBus>.Instance);
        var client = new OAuthClient(appMock.Object, Server, NullLogger<OAuthClient>.Instance, eventBus);
        return (client, appMock);
    }

    [Fact]
    public async Task ReturnsTrue_WhenAccountPresent()
    {
        var account = Mock.Of<IAccount>(a => a.HomeAccountId == new AccountId("uid.tid", "uid", "tid"));
        var (client, _) = BuildClient([account]);

        (await client.IsSignedIn()).Should().BeTrue();
    }

    [Fact]
    public async Task ReturnsFalse_WhenNoAccounts()
    {
        var (client, _) = BuildClient([]);

        (await client.IsSignedIn()).Should().BeFalse();
    }
}
