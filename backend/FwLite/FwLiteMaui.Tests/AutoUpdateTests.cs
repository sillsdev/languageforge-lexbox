using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace FwLiteMaui.Tests;

using FwLiteMaui;
#if WINDOWS
public class AutoUpdateTests
{
    private readonly AppUpdateService _appUpdateService;

    public AutoUpdateTests()
    {
        var services = new ServiceCollection()
            .AddFwLiteMauiServices(new ConfigurationManager(), Mock.Of<ILoggingBuilder>())
            .AddSingleton(Mock.Of<IPreferences>())
            .AddSingleton(Mock.Of<IConnectivity>())
            .AddSingleton<AppUpdateService>()
            .BuildServiceProvider();
        _appUpdateService = services.GetRequiredService<AppUpdateService>();
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(15, false)]
    [InlineData(22, true)]
    [InlineData(26, true)]
    [InlineData(36, true)]
    [InlineData(46, true)]
    public void ShouldCheckForUpdateAfterThreshold(int hoursInThefuture, bool shouldCheckForUpdate)
    {

    }
}
#endif
