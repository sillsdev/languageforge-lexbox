using System.Text.Json;
using System.Text.Json.Serialization;

namespace LexBoxApi.Services.FwLiteReleases;

/// <summary>
/// A release.
/// </summary>
public class GithubRelease
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
    public required string TagName { get; set; }

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
