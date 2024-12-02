using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/fwlite-release")]
public class FwLiteReleaseController(IHttpClientFactory factory, HybridCache cache) : ControllerBase
{
    private const string GithubLatestRelease = "GithubLatestRelease";

    [HttpGet("latest")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Latest()
    {
        var latestReleaseUrl = await cache.GetOrCreateAsync(GithubLatestRelease,
            LatestVersionFromGithub,
            new HybridCacheEntryOptions() { Expiration = TimeSpan.FromDays(1) });
        if (latestReleaseUrl is null) return NotFound();
        return Redirect(latestReleaseUrl);
    }

    private async ValueTask<string?> LatestVersionFromGithub(CancellationToken token)
    {
        var response = await factory.CreateClient("Github")
            .SendAsync(new HttpRequestMessage(HttpMethod.Get,
                    "https://api.github.com/repos/sillsdev/languageforge-lexbox/releases")
                {
                    Headers =
                    {
                        { "X-GitHub-Api-Version", " 2022-11-28" },
                        { "Accept", "application/vnd.github+json" },
                        { "User-Agent", "Lexbox-Release-Endpoint" }
                    }
                },
                token);
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(token);
            throw new Exception($"Failed to get latest release from github: {response.StatusCode} {responseContent}");
        }
        response.EnsureSuccessStatusCode();
        var releases = await response.Content.ReadFromJsonAsync<Release[]>(token);
        if (releases is null) return null;
        foreach (var release in releases)
        {
            if (release is { Draft: true } or { Prerelease: true })
            {
                continue;
            }
            var msixBundle = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".msixbundle", StringComparison.InvariantCultureIgnoreCase));
            if (msixBundle is not null)
            {
                return msixBundle.BrowserDownloadUrl;
            }
        }
        return null;
    }

    [HttpPost("new-release")]
    [AllowAnonymous]
    public async Task<OkResult> NewRelease()
    {
        await cache.RemoveAsync(GithubLatestRelease);
        return Ok();
    }
}

/// <summary>
/// A release.
/// </summary>
public class Release
{
    [JsonPropertyName("assets")]
    public required ReleaseAsset[] Assets { get; set; }

    [JsonPropertyName("assets_url")]
    public string? AssetsUrl { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("body_html")]
    public string? BodyHtml { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("body_text")]
    public string? BodyText { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The URL of the release discussion.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("discussion_url")]
    public string? DiscussionUrl { get; set; }

    /// <summary>
    /// true to create a draft (unpublished) release, false to create a published one.
    /// </summary>
    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("mentions_count")]
    public long? MentionsCount { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    /// <summary>
    /// Whether to identify the release as a prerelease or a full release.
    /// </summary>
    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("published_at")]
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>
    /// The name of the tag.
    /// </summary>
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("tarball_url")]
    public string? TarballUrl { get; set; }

    /// <summary>
    /// Specifies the commitish value that determines where the Git tag is created from.
    /// </summary>
    [JsonPropertyName("target_commitish")]
    public string? TargetCommitish { get; set; }

    [JsonPropertyName("upload_url")]
    public string? UploadUrl { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("zipball_url")]
    public string? ZipballUrl { get; set; }
}

/// <summary>
/// Data related to a release.
/// </summary>
public class ReleaseAsset
{
    [JsonPropertyName("browser_download_url")]
    public required string BrowserDownloadUrl { get; set; }

    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("download_count")]
    public long DownloadCount { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>
    /// The file name of the asset.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// State of the release asset.
    /// </summary>
    [JsonPropertyName("state")]
    public State State { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }


    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// State of the release asset.
/// </summary>
[JsonConverter(typeof(StateConverter))]
public enum State { Open, Uploaded };


internal class StateConverter : JsonConverter<State>
{
    public override bool CanConvert(Type t) => t == typeof(State);

    public override State Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        switch (value)
        {
            case "open":
                return State.Open;
            case "uploaded":
                return State.Uploaded;
        }

        throw new Exception("Cannot unmarshal type State");
    }

    public override void Write(Utf8JsonWriter writer, State value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case State.Open:
                JsonSerializer.Serialize(writer, "open", options);
                return;
            case State.Uploaded:
                JsonSerializer.Serialize(writer, "uploaded", options);
                return;
        }

        throw new Exception("Cannot marshal type State");
    }

}
