using FwLiteShared.Projects;
using FwLiteShared.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace FwLiteShared.Tests.Projects;

public class AdaptiveRetryPolicyTests
{
    private class FakeNetworkStatus(bool isOnline) : INetworkStatus
    {
        public bool IsOnline => isOnline;
    }

    private static RetryContext Context(long previousRetryCount) => new()
    {
        PreviousRetryCount = previousRetryCount,
        ElapsedTime = TimeSpan.Zero,
        RetryReason = new Exception("test"),
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FirstTwoAttemptsAreImmediateRegardlessOfNetwork(bool isOnline)
    {
        var policy = new LexboxProjectChangeListener.AdaptiveRetryPolicy(new FakeNetworkStatus(isOnline));

        policy.NextRetryDelay(Context(0)).Should().Be(TimeSpan.Zero);
        policy.NextRetryDelay(Context(1)).Should().Be(TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(1000)]
    public void WhileOnline_RetriesQuickly(long previousRetryCount)
    {
        var policy = new LexboxProjectChangeListener.AdaptiveRetryPolicy(new FakeNetworkStatus(isOnline: true));

        policy.NextRetryDelay(Context(previousRetryCount)).Should().Be(TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(1000)]
    public void WhileOffline_BacksOff(long previousRetryCount)
    {
        var policy = new LexboxProjectChangeListener.AdaptiveRetryPolicy(new FakeNetworkStatus(isOnline: false));

        policy.NextRetryDelay(Context(previousRetryCount)).Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void ReevaluatesNetworkStatusPerAttempt()
    {
        var online = false;
        var status = new ToggleNetworkStatus(() => online);
        var policy = new LexboxProjectChangeListener.AdaptiveRetryPolicy(status);

        policy.NextRetryDelay(Context(5)).Should().Be(TimeSpan.FromSeconds(60));
        online = true;
        policy.NextRetryDelay(Context(6)).Should().Be(TimeSpan.FromSeconds(10));
    }

    private class ToggleNetworkStatus(Func<bool> isOnline) : INetworkStatus
    {
        public bool IsOnline => isOnline();
    }

    // Returning null would end SignalR's retry loop permanently; the deliberate stop on logout belongs to
    // HandleReconnecting, never the policy.
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(50, true)]
    [InlineData(50, false)]
    public void NeverGivesUp(long previousRetryCount, bool isOnline)
    {
        var policy = new LexboxProjectChangeListener.AdaptiveRetryPolicy(new FakeNetworkStatus(isOnline));

        policy.NextRetryDelay(Context(previousRetryCount)).Should().NotBeNull();
    }
}
