using FwLiteShared;
using FwLiteShared.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FwLiteMaui.Tests;

#if WINDOWS
public class AppUpdateServiceTests
{
    private readonly AppUpdateService _appUpdateService;
    private readonly Mock<IPreferences> _preferencesMock;

    public AppUpdateServiceTests()
    {
        _preferencesMock = new Mock<IPreferences>();
        _appUpdateService = new AppUpdateService(
            Mock.Of<IHttpClientFactory>(),
            Options.Create(new FwLiteConfig()),
            Mock.Of<ILogger<AppUpdateService>>(),
            _preferencesMock.Object,
            new GlobalEventBus(Mock.Of<ILogger<GlobalEventBus>>()));
    }

    [Fact]
    public void LastUpdateCheck_ReadsFromPreferences()
    {
        var expectedTime = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        _preferencesMock.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<DateTime>(), null))
            .Returns(expectedTime);

        var result = _appUpdateService.LastUpdateCheck;

        result.Should().Be(expectedTime);
    }

    [Fact]
    public void LastUpdateCheck_SetWritesToPreferences()
    {
        var newTime = new DateTime(2024, 7, 20, 14, 0, 0, DateTimeKind.Utc);

        _appUpdateService.LastUpdateCheck = newTime;

        _preferencesMock.Verify(p => p.Set("lastUpdateChecked", newTime, null), Times.Once);
    }

    [Fact]
    public void SupportsAutoUpdate_ReturnsTrueForNonPortableApp()
    {
        var result = _appUpdateService.SupportsAutoUpdate;

        result.Should().Be(!FwLiteMauiKernel.IsPortableApp);
    }
}
#endif
