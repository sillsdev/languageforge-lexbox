using LexCore.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FwLiteShared.Tests;

public class FwLiteConfigTests
{
    //same order as the apps: AddFwLiteShared binds the "FwLite" section, then the host's Configure sets AppVersion and Os
    private static FwLiteConfig BuildConfig(Dictionary<string, string?> configValues, Action<FwLiteConfig> hostConfigure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(configValues).Build());
        services.AddOptions<FwLiteConfig>().BindConfiguration("FwLite");
        services.Configure(hostConfigure);
        return services.BuildServiceProvider().GetRequiredService<IOptions<FwLiteConfig>>().Value;
    }

    [Fact]
    public void ResolveUpdateUrl_UsesAppVersionSetAfterBinding_WhenFwLiteSectionHasAnUnrelatedKey()
    {
        //any key in the section makes the binder visit every property; this one once froze appVersion=Unknown into the URL
        var config = BuildConfig(new() { ["FwLite:UpdateCheckCondition"] = "Always" },
            c =>
            {
                c.AppVersion = "v2026-11-13-9058284e";
                c.Os = FwLitePlatform.Windows;
            });

        config.ResolveUpdateUrl().Should()
            .Be("https://lexbox.org/api/fwlite-release/should-update?appVersion=v2026-11-13-9058284e&edition=Windows");
    }

    [Fact]
    public void ResolveUpdateUrl_ReturnsConfiguredOverride()
    {
        var config = BuildConfig(new() { ["FwLite:OverrideUpdateUrl"] = "http://localhost:5555/api/fwlite-release/should-update" },
            c => c.AppVersion = "v2026-11-13-9058284e");

        config.ResolveUpdateUrl().Should().Be("http://localhost:5555/api/fwlite-release/should-update");
    }

    [Fact]
    public void Edition_FollowsOsSetAfterBinding()
    {
        var config = BuildConfig(new() { ["FwLite:UpdateCheckCondition"] = "Always" },
            c => c.Os = FwLitePlatform.Android);

        config.Edition.Should().Be(FwLiteEdition.Android);
    }
}
