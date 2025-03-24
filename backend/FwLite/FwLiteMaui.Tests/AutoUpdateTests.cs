namespace FwLiteMaui.Tests;

using FwLiteMaui;

public class AutoUpdateTests
{
    private readonly AppUpdateService _appUpdateService;

    public AutoUpdateTests()
    {
        var services = new ServiceCollection()
            .AddSingleton<AppUpdateService>()
            .AddHttpClient()
            //...
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
