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

    private UpdateChecker CreateUpdateChecker(FwLiteConfig? config = null)
    {
        return new UpdateChecker(
            _httpClientFactoryMock.Object,
            Mock.Of<ILogger<UpdateChecker>>(),
            Options.Create(config ?? new FwLiteConfig()),
            new GlobalEventBus(Mock.Of<ILogger<GlobalEventBus>>()),
            _platformUpdateServiceMock.Object,
            new MemoryCache(new MemoryCacheOptions()));
    }

    [Fact]
    public void ShouldCheckForUpdate_WhenConfigSetToNever_ReturnsFalse()
    {
        var config = new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.Never };
        var checker = CreateUpdateChecker(config);

        var result = checker.ShouldCheckForUpdate();

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldCheckForUpdate_WhenConfigSetToAlways_ReturnsTrue()
    {
        var config = new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.Always };
        var checker = CreateUpdateChecker(config);

        var result = checker.ShouldCheckForUpdate();

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(9, true)]
    [InlineData(24, true)]
    [InlineData(28, true)]
    public void ShouldCheckForUpdate_RespectsDefaultInterval(int hoursSinceLastCheck, bool expectedResult)
    {
        var config = new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.OnInterval };
        var lastCheckTime = DateTime.UtcNow.AddHours(-hoursSinceLastCheck);
        _platformUpdateServiceMock.Setup(p => p.LastUpdateCheck).Returns(lastCheckTime);

        var checker = CreateUpdateChecker(config);
        var result = checker.ShouldCheckForUpdate();

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    public void ShouldCheckForUpdate_RespectsCustomInterval(int hoursSinceLastCheck, bool expectedResult)
    {
        var config = new FwLiteConfig
        {
            UpdateCheckCondition = UpdateCheckCondition.OnInterval,
            UpdateCheckInterval = TimeSpan.FromHours(4)
        };
        var lastCheckTime = DateTime.UtcNow.AddHours(-hoursSinceLastCheck);
        _platformUpdateServiceMock.Setup(p => p.LastUpdateCheck).Returns(lastCheckTime);

        var checker = CreateUpdateChecker(config);
        var result = checker.ShouldCheckForUpdate();

        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ShouldCheckForUpdate_WhenLastCheckInFuture_ReturnsTrue()
    {
        var config = new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.OnInterval };
        var futureTime = DateTime.UtcNow.AddHours(2);
        _platformUpdateServiceMock.Setup(p => p.LastUpdateCheck).Returns(futureTime);

        var checker = CreateUpdateChecker(config);
        var result = checker.ShouldCheckForUpdate();

        result.Should().BeTrue("because a future timestamp indicates clock skew and should trigger a check");
    }

    [Fact]
    public void ShouldCheckForUpdate_WhenNeverCheckedBefore_ReturnsTrue()
    {
        var config = new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.OnInterval };
        _platformUpdateServiceMock.Setup(p => p.LastUpdateCheck).Returns(DateTime.MinValue);

        var checker = CreateUpdateChecker(config);
        var result = checker.ShouldCheckForUpdate();

        result.Should().BeTrue("because DateTime.MinValue means never checked");
    }
}
