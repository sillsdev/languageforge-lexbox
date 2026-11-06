using System.Diagnostics;
using LexBoxApi.Config;
using LexBoxApi.Otel;
using LexCore.Entities;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Services.FwLiteReleases;

public class FwLiteReleaseService(IHttpClientFactory factory, HybridCache cache, IOptions<FwLiteReleaseConfig> config)
{
    public const string HttpClientName = "Github";
    private const string GithubLatestRelease = "GithubLatestRelease";
    public const string FwLiteClientVersionTag = "app.fw-lite.client.version";
    public const string FwLiteEditionTag = "app.fw-lite.edition";
    public const string FwLiteReleaseVersionTag = "app.fw-lite.release.version";

    public async ValueTask<FwLiteRelease?> GetLatestRelease(FwLiteEdition edition, CancellationToken token = default)
    {
        return await cache.GetOrCreateAsync($"{GithubLatestRelease}|{edition}",
            edition,
            FetchLatestReleaseFromGithub,
            new HybridCacheEntryOptions() { Expiration = TimeSpan.FromDays(1) },
            cancellationToken: token, tags: [GithubLatestRelease]);
    }

    public async ValueTask<ShouldUpdateResponse> ShouldUpdate(FwLiteEdition edition, string appVersion)
    {
        var latestRelease = await GetLatestRelease(edition);
        if (latestRelease is null) return new ShouldUpdateResponse(null);

        return FwLiteVersion.IsNewer(latestRelease.Version, appVersion)
            ? new ShouldUpdateResponse(latestRelease)
            : new ShouldUpdateResponse(null);
    }

    public async ValueTask InvalidateReleaseCache()
    {
        await cache.RemoveByTagAsync(GithubLatestRelease);
    }

    private async ValueTask<FwLiteRelease?> FetchLatestReleaseFromGithub(FwLiteEdition edition, CancellationToken token)
    {
        var editionConfig = config.Value.Editions.GetValueOrDefault(edition);
        if (editionConfig is null)
        {
            //Editions with no release-asset config (e.g. store-distributed iOS/Mac) have no GitHub
            //feed to check. Return null ("no update") rather than throwing, which the endpoint would
            //otherwise surface as a 500 on every client launch.
            return null;
        }
        using var activity = LexBoxActivitySource.Get().StartActivity();
        activity?.AddTag(FwLiteEditionTag, edition.ToString());
        var response = await factory.CreateClient(HttpClientName)
            .SendAsync(new HttpRequestMessage(HttpMethod.Get,
                    "https://api.github.com/repos/sillsdev/languageforge-lexbox/releases")
                {
                    Headers =
                    {
                        { "X-GitHub-Api-Version", "2022-11-28" },
                        { "Accept", "application/vnd.github+json" },
                        { "User-Agent", "Lexbox-Release-Endpoint" }
                    }
                },
                token);
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(token);
            var e = new Exception($"Failed to get latest release from github: {response.StatusCode} {responseContent}");
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);
            activity?.AddException(e);
            throw e;
        }

        response.EnsureSuccessStatusCode();
        var releases = await response.Content.ReadFromJsonAsync<GithubRelease[]>(token);
        if (releases is not null)
        {
            foreach (var release in releases)
            {
                if (release is { Draft: true } or { Prerelease: true })
                {
                    continue;
                }

                var releaseAsset = release.Assets.FirstOrDefault(a => editionConfig.FileName.IsMatch(a.Name));
                if (releaseAsset is not null)
                {
                    activity?.AddTag(FwLiteReleaseVersionTag, release.TagName);
                    return new FwLiteRelease(release.TagName, releaseAsset.BrowserDownloadUrl);
                }
            }
        }

        activity?.SetStatus(ActivityStatusCode.Error, "No release found");
        activity?.AddTag(FwLiteReleaseVersionTag, null);
        return null;
    }
}
