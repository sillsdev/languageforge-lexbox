using System.Net.Http.Headers;
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
            .AddHttpClient(FwLiteReleaseService.HttpClientName,
                client =>
                {
                    var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
                    if (githubToken is not null)
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
                    }
                })
            .Services
            .AddOptions<FwLiteReleaseConfig>().Configure(config =>
            {
                config.Editions.Add(FwLiteEdition.Windows, new FwLiteEditionConfig() { FileNameRegex = "(?i)\\.msixbundle$" });
                config.Editions.Add(FwLiteEdition.Linux, new FwLiteEditionConfig() { FileNameRegex = "(?i)linux\\.zip$" });
            })
            .Services
            .AddHybridCache()
            .Services.BuildServiceProvider();
#pragma warning restore EXTEXP0018
        _fwLiteReleaseService = services.GetRequiredService<FwLiteReleaseService>();
    }

    [Theory]
    [InlineData(FwLiteEdition.Windows)]
    [InlineData(FwLiteEdition.Linux)]
    public async Task CanGetLatestRelease(FwLiteEdition edition)
    {
        var latestRelease = await _fwLiteReleaseService.GetLatestRelease(edition);
        latestRelease.Should().NotBeNull();
        latestRelease.Version.Should().NotBeNullOrEmpty();
        latestRelease.Url.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(FwLiteEdition.iOS)]
    [InlineData(FwLiteEdition.Mac)]
    [InlineData(FwLiteEdition.Android)]
    public async Task UnconfiguredEditionReturnsNullInsteadOfThrowing(FwLiteEdition edition)
    {
        //Editions with no release-asset config (only Windows + Linux are configured in this fixture)
        //must not throw: the should-update endpoint would otherwise 500 on every such client launch.
        var latestRelease = await _fwLiteReleaseService.GetLatestRelease(edition);
        latestRelease.Should().BeNull();

        var shouldUpdate = await _fwLiteReleaseService.ShouldUpdate(edition, "v2024-11-20-d04e9b96");
        shouldUpdate.Should().NotBeNull();
        shouldUpdate.Release.Should().BeNull();
        shouldUpdate.Update.Should().BeFalse();
    }

    [Theory]
    [InlineData("v2024-11-20-d04e9b96")]
    public async Task IsConsideredAnOldVersion(string appVersion)
    {
        var shouldUpdate = await _fwLiteReleaseService.ShouldUpdate(FwLiteEdition.Windows, appVersion);
        shouldUpdate.Should().NotBeNull();
        shouldUpdate.Release.Should().NotBeNull();
        shouldUpdate.Update.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldUpdateWithLatestVersionShouldReturnFalse()
    {
        var latestRelease = await _fwLiteReleaseService.GetLatestRelease(FwLiteEdition.Windows);
        latestRelease.Should().NotBeNull();
        var shouldUpdate = await _fwLiteReleaseService.ShouldUpdate(FwLiteEdition.Windows, latestRelease.Version);
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
    [InlineData(
        "v2026-09-07-38233379",
        "fw-lite-v2026-09-28-553eb3f8",
        true,
        "a client built before the tag prefix still sees a newer prefixed release")]
    [InlineData(
        "fw-lite-v2026-09-28-553eb3f8",
        "v2026-09-07-38233379",
        false,
        "an older bare release is never an update for a prefixed client (ordinal order alone would say it is)")]
    [InlineData(
        "fw-lite-v2026-09-28-553eb3f8",
        "fw-lite-v2026-09-28-553eb3f8",
        false,
        "same prefixed version is not an update")]
    public void IsNewerGivesExpectedResult(string appVersion,
        string latestVersion,
        bool expected,
        string reason)
    {
        var actual = FwLiteVersion.IsNewer(latestVersion, appVersion);
        actual.Should().Be(expected, reason);
    }
}
