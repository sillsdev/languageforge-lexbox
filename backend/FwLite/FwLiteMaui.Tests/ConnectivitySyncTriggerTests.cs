using FwLiteMaui.Services;

namespace FwLiteMaui.Tests;

public class ConnectivitySyncTriggerTests
{
    [Theory]
    [InlineData(NetworkAccess.None, NetworkAccess.Internet, true)]
    [InlineData(NetworkAccess.Unknown, NetworkAccess.Internet, true)]
    [InlineData(NetworkAccess.ConstrainedInternet, NetworkAccess.Internet, true)]
    [InlineData(NetworkAccess.Internet, NetworkAccess.Internet, false)] // already online — don't re-run
    [InlineData(NetworkAccess.Internet, NetworkAccess.None, false)]     // going offline
    [InlineData(NetworkAccess.None, NetworkAccess.Local, false)]        // local network only, no internet
    public void ShouldRecover_OnlyOnTransitionIntoInternet(NetworkAccess previous, NetworkAccess current, bool expected)
    {
        ConnectivitySyncTrigger.ShouldRecover(previous, current).Should().Be(expected);
    }
}
