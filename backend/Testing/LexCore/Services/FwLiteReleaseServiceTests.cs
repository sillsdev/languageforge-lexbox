using System.Net.Http.Headers;
using System.Xml.Linq;
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
    public void ShouldUpdateToReleaseGivesExpectedResult(string appVersion,
        string latestVersion,
        bool expected,
        string reason)
    {
        var actual = FwLiteReleaseService.ShouldUpdateToRelease(appVersion, latestVersion);
        actual.Should().Be(expected, reason);
    }

    [Fact]
    public async Task AppInstallerSelfReferencingUriEndsInAppinstaller()
    {
        //The App Installer APIs (Add-AppxPackage -AppInstallerFile, AddPackageByAppInstallerFileAsync)
        //validate that the update source URL's path ends in .appinstaller. This root Uri is baked into
        //every install as its update source, so if it stops ending in .appinstaller auto-update breaks.
        var appInstaller = await _fwLiteReleaseService.GenerateAppInstaller();
        var uri = XDocument.Parse(appInstaller).Root!.Attribute("Uri")!.Value;
        new Uri(uri).AbsolutePath.Should().EndWith(".appinstaller");
    }

    [Theory]
    //must match the bundle identity version CI stamps: `date +%Y.%-m.%-d` (no leading zeros) + ".1"
    [InlineData("v2025-01-17-a62c709c", "2025.1.17.1")]
    [InlineData("v2026-07-06-915ca19d", "2026.7.6.1")]
    [InlineData("v2026-10-30-deadbeef", "2026.10.30.1")]
    public void ConvertVersionToAppInstallerVersionGivesExpectedResult(string tag, string expected)
    {
        FwLiteReleaseService.ConvertVersionToAppInstallerVersion(tag).Should().Be(expected);
    }
}
