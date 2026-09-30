using FwLiteShared;
using FwLiteShared.AppUpdate;
using FwLiteShared.Events;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FwLiteShared.Tests.AppUpdate;

public class UpdateCheckerTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IPlatformUpdateService> _platformUpdateServiceMock = new();

    // Empty preferences => never checked => the shared throttle allows a check. This lets the tests below
    // isolate ShouldCheckReleaseFeed's platform-skip logic from the interval gate (covered by
    // UpdateCheckThrottleTests).
    private readonly InMemoryPreferencesService _preferences = new();

    private UpdateChecker CreateUpdateChecker(FwLiteConfig? config = null)
    {
        var options = Options.Create(config ?? new FwLiteConfig());
        var throttle = new UpdateCheckThrottle(_preferences, options, Mock.Of<ILogger<UpdateCheckThrottle>>());
        return new UpdateChecker(
            _httpClientFactoryMock.Object,
            Mock.Of<ILogger<UpdateChecker>>(),
            options,
            new GlobalEventBus(Mock.Of<ILogger<GlobalEventBus>>()),
            _platformUpdateServiceMock.Object,
            throttle,
            new MemoryCache(new MemoryCacheOptions()));
    }

    [Theory]
    [InlineData(FwLitePlatform.iOS)]
    [InlineData(FwLitePlatform.Mac)]
    [InlineData(FwLitePlatform.Android)]
    public void ShouldCheckReleaseFeed_WhenStoreDistributedPlatform_ReturnsFalse(FwLitePlatform os)
    {
        //iOS/Mac have no GitHub release feed and Android is Play-driven; the feed round-trip is pointless.
        //Preferences are empty (never checked), so the throttle would otherwise allow the check - this
        //proves the platform skip wins for the default OnInterval condition.
        var config = new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.OnInterval, Os = os };
        var checker = CreateUpdateChecker(config);

        checker.ShouldCheckReleaseFeed().Should().BeFalse();
    }

    [Theory]
    [InlineData(FwLitePlatform.iOS)]
    [InlineData(FwLitePlatform.Mac)]
    [InlineData(FwLitePlatform.Android)]
    public void ShouldCheckReleaseFeed_WhenAlwaysConfigured_OverridesStoreDistributedSkip(FwLitePlatform os)
    {
        //The explicit Always override still forces a feed check (used for testing the endpoint on device).
        var config = new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.Always, Os = os };
        var checker = CreateUpdateChecker(config);

        checker.ShouldCheckReleaseFeed().Should().BeTrue();
    }

    [Fact]
    public void ShouldCheckReleaseFeed_WhenNonStorePlatform_DelegatesToThrottle()
    {
        //Windows uses the feed; with an empty last-check the throttle allows the check.
        var config = new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.OnInterval, Os = FwLitePlatform.Windows };
        var checker = CreateUpdateChecker(config);

        checker.ShouldCheckReleaseFeed().Should().BeTrue();
    }

    [Fact]
    public void ShouldCheckReleaseFeed_WhenConfigSetToNever_ReturnsFalse()
    {
        var config = new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.Never, Os = FwLitePlatform.Windows };
        var checker = CreateUpdateChecker(config);

        checker.ShouldCheckReleaseFeed().Should().BeFalse();
    }
}
