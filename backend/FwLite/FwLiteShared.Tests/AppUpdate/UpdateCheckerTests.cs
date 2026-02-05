using FwLiteShared;
using FwLiteShared.AppUpdate;
using FwLiteShared.Events;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

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

    [Fact]
    public async Task CheckForUpdate_WhenPlatformHandlesOwnCheck_CallsPlatformCheckForUpdateAsync()
    {
        var config = new FwLiteConfig();
        var expectedUpdate = new AvailableUpdate(
            new LexCore.Entities.FwLiteRelease("123", ""),
            SupportsAutoUpdate: true);

        _platformUpdateServiceMock.Setup(p => p.HandlesOwnUpdateCheck).Returns(true);
        _platformUpdateServiceMock.Setup(p => p.CheckForUpdateAsync()).ReturnsAsync(expectedUpdate);

        var checker = CreateUpdateChecker(config);
        var result = await checker.CheckForUpdate();

        result.Should().Be(expectedUpdate);
        _platformUpdateServiceMock.Verify(p => p.CheckForUpdateAsync(), Times.Once);
    }

    [Fact]
    public async Task CheckForUpdate_WhenPlatformHandlesOwnCheck_UpdatesLastUpdateCheck()
    {
        var config = new FwLiteConfig();
        _platformUpdateServiceMock.Setup(p => p.HandlesOwnUpdateCheck).Returns(true);
        _platformUpdateServiceMock.Setup(p => p.CheckForUpdateAsync()).ReturnsAsync((AvailableUpdate?)null);

        var checker = CreateUpdateChecker(config);
        await checker.CheckForUpdate();

        _platformUpdateServiceMock.VerifySet(p => p.LastUpdateCheck = It.Is<DateTime>(d =>
            d > DateTime.UtcNow.AddMinutes(-1) && d <= DateTime.UtcNow), Times.Once);
    }

    [Fact]
    public async Task CheckForUpdate_WhenPlatformDoesNotHandleOwnCheck_DoesNotCallPlatformCheckForUpdateAsync()
    {
        var config = new FwLiteConfig { UpdateUrl = "http://example.com/update" };
        _platformUpdateServiceMock.Setup(p => p.HandlesOwnUpdateCheck).Returns(false);

        // Mock HTTP client to avoid actual network call
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"release\":null}")
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://example.com") };
        _httpClientFactoryMock.Setup(f => f.CreateClient("Lexbox")).Returns(httpClient);

        var checker = CreateUpdateChecker(config);
        await checker.CheckForUpdate();

        _platformUpdateServiceMock.Verify(p => p.CheckForUpdateAsync(), Times.Never);
    }
}
