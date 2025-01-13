using FluentAssertions;
using LexBoxApi;
using LexBoxApi.Config;
using LexBoxApi.Services.FwLiteReleases;
using LexCore.Entities;
using Microsoft.Extensions.DependencyInjection;
using Testing.Fixtures;

namespace Testing.LexCore.Services;

public class FwLiteReleaseServiceTests
{
    private readonly FwLiteReleaseService _fwLiteReleaseService;

    public FwLiteReleaseServiceTests()
    {
        //disable warning about hybrid cache being experimental
#pragma warning disable EXTEXP0018
        var services = new ServiceCollection()
            .AddSingleton<FwLiteReleaseService>()
            .AddHttpClient()
            .AddOptions<FwLiteReleaseConfig>().Configure(config =>
            {
                config.Platforms.Add(FwLitePlatform.Windows, new FwLitePlatformConfig() { FileNameRegex = "(?i)\\.msixbundle$" });
                config.Platforms.Add(FwLitePlatform.Linux, new FwLitePlatformConfig() { FileNameRegex = "(?i)linux\\.zip$" });
            })
            .Services
            .AddHybridCache()
            .Services.BuildServiceProvider();
#pragma warning restore EXTEXP0018
        _fwLiteReleaseService = services.GetRequiredService<FwLiteReleaseService>();
    }

    [Theory]
    [InlineData(FwLitePlatform.Windows)]
    [InlineData(FwLitePlatform.Linux)]
    public async Task CanGetLatestRelease(FwLitePlatform platform)
    {
        var latestRelease = await _fwLiteReleaseService.GetLatestRelease(platform);
        latestRelease.Should().NotBeNull();
        latestRelease.Version.Should().NotBeNullOrEmpty();
        latestRelease.Url.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("v2024-11-20-d04e9b96")]
    public async Task IsConsideredAnOldVersion(string appVersion)
    {
        var shouldUpdate = await _fwLiteReleaseService.ShouldUpdate(FwLitePlatform.Windows, appVersion);
        shouldUpdate.Should().NotBeNull();
        shouldUpdate.Release.Should().NotBeNull();
        shouldUpdate.Update.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldUpdateWithLatestVersionShouldReturnFalse()
    {
        var latestRelease = await _fwLiteReleaseService.GetLatestRelease(FwLitePlatform.Windows);
        latestRelease.Should().NotBeNull();
        var shouldUpdate = await _fwLiteReleaseService.ShouldUpdate(FwLitePlatform.Windows, latestRelease.Version);
        shouldUpdate.Should().NotBeNull();
        shouldUpdate.Release.Should().BeNull();
        shouldUpdate.Update.Should().BeFalse();
    }

    [Theory]
    [InlineData(
        "v2024-11-20-d04e9b96",
        "v2024-11-20-d04e9b96",
        false,
        "there's no need to update when you have the latest version")]
    [InlineData(
        "v2024-11-20-d04e9b96",
        "v2024-11-27-c54f64d1",
        true,
        "there's a need to update when you have an older version")]
    public void ShouldUpdateToReleaseGivesExpectedResult(string appVersion,
        string latestVersion,
        bool expected,
        string reason)
    {
        var actual = FwLiteReleaseService.ShouldUpdateToRelease(appVersion, latestVersion);
        actual.Should().Be(expected, reason);
    }
}
