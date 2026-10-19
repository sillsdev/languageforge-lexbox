using FwLiteShared.Analytics;
using FwLiteShared.Auth;
using FwLiteShared.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FwLiteShared.Tests.Analytics;

public class AnalyticsIdentityListenerTests
{
    private static readonly LexboxServer LexboxOrg = new(new Uri("https://lexbox.org"), "Lexbox");

    [Fact]
    public async Task LogoutDuringInFlightLogin_EndsLoggedOut()
    {
        var calls = new List<string>();
        var analytics = new Mock<IAnalyticsService>();
        analytics.Setup(a => a.Identify(It.IsAny<string>()))
            .Callback<string>(userId => { lock (calls) calls.Add($"identify:{userId}"); });
        analytics.Setup(a => a.Reset())
            .Callback(() => { lock (calls) calls.Add("reset"); });

        var listener = CreateListener(analytics.Object);

        // A login whose user lookup is still in flight — it holds the identity gate while awaiting.
        var pendingUser = new TaskCompletionSource<LexboxUser?>();
        // The TaskCompletionSource is the interleaving control, so awaiting it is intentional here.
#pragma warning disable VSTHRD003
        var loginTask = listener.RunIdentityOperation(LexboxOrg, AuthenticationChangeCause.Login, () => pendingUser.Task);
#pragma warning restore VSTHRD003

        // A logout arriving mid-lookup must queue behind the gate, not run ahead of the login.
        var logoutTask = listener.RunIdentityOperation(LexboxOrg, AuthenticationChangeCause.Logout,
            () => Task.FromResult<LexboxUser?>(null));
        logoutTask.IsCompleted.Should().BeFalse();

        // The lookup resolves with an account that is already stale (the user has logged out).
        pendingUser.SetResult(new LexboxUser("Ada", "user-1"));
        await Task.WhenAll(loginTask, logoutTask);

        // Serialization forces the logout's Reset to run after the stale Identify, so the final
        // state is logged out — not the stale account restored on top of the logout.
        calls.Should().Equal("identify:user-1", "reset");
    }

    [Fact]
    public async Task LoginThenLogout_RunSequentially_EndLoggedOut()
    {
        var calls = new List<string>();
        var analytics = new Mock<IAnalyticsService>();
        analytics.Setup(a => a.Identify(It.IsAny<string>()))
            .Callback<string>(userId => calls.Add($"identify:{userId}"));
        analytics.Setup(a => a.Reset()).Callback(() => calls.Add("reset"));

        var listener = CreateListener(analytics.Object);

        await listener.RunIdentityOperation(LexboxOrg, AuthenticationChangeCause.Login,
            () => Task.FromResult<LexboxUser?>(new LexboxUser("Ada", "user-1")));
        await listener.RunIdentityOperation(LexboxOrg, AuthenticationChangeCause.Logout,
            () => Task.FromResult<LexboxUser?>(null));

        calls.Should().Equal("identify:user-1", "reset");
    }

    [Fact]
    public async Task Logout_DoesNotInvokeUserLookup()
    {
        var analytics = new Mock<IAnalyticsService>();
        var listener = CreateListener(analytics.Object);
        var lookupCalled = false;

        await listener.RunIdentityOperation(LexboxOrg, AuthenticationChangeCause.Logout,
            () => { lookupCalled = true; return Task.FromResult<LexboxUser?>(null); });

        lookupCalled.Should().BeFalse();
        analytics.Verify(a => a.Reset(), Times.Once);
        analytics.Verify(a => a.Identify(It.IsAny<string>()), Times.Never);
    }

    private static AnalyticsIdentityListener CreateListener(IAnalyticsService analytics)
    {
        var clientFactory = new OAuthClientFactory(
            Mock.Of<IServiceProvider>(),
            Options.Create(new AuthConfig { ClientId = "test", LexboxServers = [] }),
            Mock.Of<ILogger<OAuthClientFactory>>());

        return new AnalyticsIdentityListener(
            analytics,
            clientFactory,
            Options.Create(new AuthConfig { ClientId = "test", LexboxServers = [] }),
            new GlobalEventBus(Mock.Of<ILogger<GlobalEventBus>>()),
            Mock.Of<ILogger<AnalyticsIdentityListener>>());
    }
}
