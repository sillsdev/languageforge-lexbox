using FwLiteShared.Events;
using Microsoft.Extensions.Logging;
using Moq;

namespace FwLiteMaui.Tests;

#if WINDOWS
public class AppUpdateServiceTests
{
    private readonly AppUpdateService _appUpdateService;

    public AppUpdateServiceTests()
    {
        _appUpdateService = new AppUpdateService(
            Mock.Of<ILogger<AppUpdateService>>(),
            new GlobalEventBus(Mock.Of<ILogger<GlobalEventBus>>()));
    }

    [Fact]
    public void SupportsAutoUpdate_ReturnsTrueForNonPortableApp()
    {
        var result = _appUpdateService.SupportsAutoUpdate;

        result.Should().Be(!FwLiteMauiKernel.IsPortableApp);
    }
}
#endif
