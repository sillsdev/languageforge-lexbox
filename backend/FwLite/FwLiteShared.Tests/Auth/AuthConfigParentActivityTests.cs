using FwLiteShared.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FwLiteShared.Tests.Auth;

public class AuthConfigParentActivityTests
{
    [Fact]
    public void GetParentActivityOrWindow_RunsAtCallTime_NotWhenOptionsAreBuilt()
    {
        object? current = null;
        var services = new ServiceCollection();
        services.AddOptions<AuthConfig>().Configure(c =>
        {
            c.LexboxServers = [];
            c.ClientId = AuthConfig.DefaultClientId;
            c.GetParentActivityOrWindow = () => current;
        });
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<AuthConfig>>();

        // First resolve happens at Maui startup, before Platform.OnResume sets CurrentActivity.
        _ = options.Value;
        options.Value.GetParentActivityOrWindow.Should().NotBeNull();
        options.Value.GetParentActivityOrWindow!().Should().BeNull();

        current = new object();
        options.Value.GetParentActivityOrWindow!().Should().BeSameAs(current);
    }
}
