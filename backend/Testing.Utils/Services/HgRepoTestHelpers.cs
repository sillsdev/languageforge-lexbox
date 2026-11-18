using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Testing.ApiTests;
using Testing.Fixtures;

namespace Testing.Services;

/// <summary>
/// The tip of a repo as served by hgweb at /hg/{code}/file/tip?style=json-lex: the tip node plus the top-level entries of its manifest.
/// </summary>
public record HgTip(string Node, string[] Files, string[] Directories)
{
    public bool IsEmpty => Node == HgRepoTestHelpers.AllZeroHash;
}

/// <summary>
/// Repo related project fields as exposed by GraphQL.
/// </summary>
public record ProjectRepoState(
    DateTimeOffset? LastCommit,
    int? RepoSizeInKb,
    int? LexEntryCount,
    bool HasAbandonedTransactions,
    string ResetStatus);

public record RawResponse(HttpStatusCode Status, string Body)
{
    public bool IsSuccessStatusCode => (int)Status is >= 200 and <= 299;
}

/// <summary>
/// Helpers that observe hg repos only through LexBox's public surface (hgweb proxy, REST, GraphQL, TUS), never via disk.
/// </summary>
public static class HgRepoTestHelpers
{
    public const string AllZeroHash = "0000000000000000000000000000000000000000";
    public static readonly TimeSpan DefaultPollTimeout = TimeSpan.FromSeconds(30);

    public static async Task<HgTip> GetTip(this ApiTestBase apiTester, string projectCode)
    {
        var response = await apiTester.HttpClient.GetAsync($"hg/{projectCode}/file/tip?style=json-lex");
        response.ShouldBeSuccessful();
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        json.Should().NotBeNull();
        var node = json["node"]?.GetValue<string>();
        node.Should().NotBeNullOrEmpty();
        return new HgTip(node, AbsPaths(json["files"]), AbsPaths(json["directories"]));
    }

    private static string[] AbsPaths(JsonNode? entries)
    {
        return entries?.AsArray().Select(e => e?["abspath"]?.GetValue<string>() ?? "").ToArray() ?? [];
    }

    public static async Task AssertRepoEmpty(this ApiTestBase apiTester, string projectCode)
    {
        var tip = await apiTester.GetTip(projectCode);
        tip.Node.Should().Be(AllZeroHash);
        tip.Files.Should().BeEmpty();
        tip.Directories.Should().BeEmpty();
    }

    /// <summary>
    /// hgweb caches its list of repos for its refreshinterval (5s), and a repo removed within that window gives a 500
    /// rather than a 404, so this polls until hgweb reports the repo as not found.
    /// </summary>
    public static async Task AssertRepoNotServed(this ApiTestBase apiTester, string projectCode)
    {
        var response = await PollUntil(
            () => apiTester.SendWithoutRetry(HttpMethod.Get, $"hg/{projectCode}/file/tip?style=json-lex"),
            r => r.Status == HttpStatusCode.NotFound);
        response.Status.Should().Be(HttpStatusCode.NotFound,
            $"repo {projectCode} should no longer be served by hgweb, response body: {response.Body}");
    }

    /// <summary>
    /// Starts a project reset and asserts the repo was replaced with an empty one
    /// </summary>
    public static async Task StartResetAndAssertEmpty(this ApiTestBase apiTester, string projectCode)
    {
        // The backup of a reset repo is named with a timestamp with a resolution of seconds,
        // so resetting the same project twice within a second would collide
        await Task.Delay(1100);
        await apiTester.StartLexboxProjectReset(projectCode);
        (await apiTester.GetProjectRepoState(projectCode)).ResetStatus.Should().Be("IN_PROGRESS");
        await apiTester.AssertRepoEmpty(projectCode);
    }

    /// <summary>
    /// Fills a project's repo with the template repo by resetting it and uploading <see cref="IntegrationFixture.TemplateRepoZip"/>
    /// </summary>
    public static async Task PopulateWithTemplateRepo(this IntegrationFixture fixture, string projectCode)
    {
        await fixture.AdminApiTester.StartResetAndAssertEmpty(projectCode);
        await fixture.FinishLexboxProjectResetWithTemplateRepo(projectCode);
        (await fixture.AdminApiTester.GetTip(projectCode)).Should().BeEquivalentTo(IntegrationFixture.TemplateRepoTip);
    }

    public static async Task<byte[]> DownloadBackup(this ApiTestBase apiTester, string projectCode)
    {
        var response = await apiTester.HttpClient.GetAsync($"api/project/backupProject/{projectCode}");
        response.ShouldBeSuccessful();
        (response.Content.Headers.ContentType?.MediaType).Should().Be("application/zip");
        return await response.Content.ReadAsByteArrayAsync();
    }

    public static string[] ZipEntryNames(byte[] zip)
    {
        using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        return archive.Entries.Select(e => e.FullName).ToArray();
    }

    /// <summary>
    /// hgweb only shows what's committed, so this uses a backup to verify that nothing but the .hg folder is stored for the project
    /// </summary>
    public static async Task AssertRepoContainsOnlyHgFolder(this ApiTestBase apiTester, string projectCode)
    {
        var entryNames = ZipEntryNames(await apiTester.DownloadBackup(projectCode));
        entryNames.Should().NotBeEmpty();
        entryNames.Should().OnlyContain(name => name.StartsWith(".hg/"));
    }

    public static async Task<ProjectRepoState> GetProjectRepoState(this ApiTestBase apiTester, string projectCode)
    {
        var json = await apiTester.ExecuteGql($$"""
            query {
                projectByCode(code: "{{projectCode}}") {
                    code
                    lastCommit
                    repoSizeInKb
                    resetStatus
                    hasAbandonedTransactions
                    flexProjectMetadata {
                        lexEntryCount
                    }
                }
            }
            """);
        var projectNode = json["data"]?["projectByCode"];
        var project = projectNode.Should().BeOfType<JsonObject>().Subject;
        var lastCommit = project["lastCommit"]?.ToString();
        return new ProjectRepoState(
            lastCommit is null ? null : DateTimeOffset.Parse(lastCommit, CultureInfo.InvariantCulture),
            project["repoSizeInKb"]?.GetValue<int>(),
            project["flexProjectMetadata"]?["lexEntryCount"]?.GetValue<int>(),
            project["hasAbandonedTransactions"]!.GetValue<bool>(),
            project["resetStatus"]!.GetValue<string>());
    }

    /// <summary>
    /// For metadata that's updated by a background job (e.g. UpdateProjectMetadataJob after a push).
    /// Returns the last state seen, callers must assert on it.
    /// </summary>
    public static Task<ProjectRepoState> WaitForProjectRepoState(this ApiTestBase apiTester,
        string projectCode,
        Func<ProjectRepoState, bool> isDone)
    {
        return PollUntil(() => apiTester.GetProjectRepoState(projectCode), isDone);
    }

    public static async Task<T> PollUntil<T>(Func<Task<T>> get, Func<T, bool> isDone, TimeSpan? timeout = null)
    {
        var giveUpAt = DateTime.UtcNow + (timeout ?? DefaultPollTimeout);
        var value = await get();
        while (!isDone(value) && DateTime.UtcNow < giveUpAt)
        {
            await Task.Delay(500);
            value = await get();
        }
        return value;
    }

    /// <summary>
    /// Sends a request without the retry pipeline of <see cref="ApiTestBase.HttpClient"/>, which retries 5xx responses.
    /// Use it to observe error responses exactly as the server returned them.
    /// </summary>
    public static async Task<RawResponse> SendWithoutRetry(this ApiTestBase apiTester, HttpMethod method, string url)
    {
        using var request = new HttpRequestMessage(method, url);
        return await SendWithoutRetry(apiTester, request);
    }

    private static async Task<RawResponse> SendWithoutRetry(ApiTestBase apiTester, HttpRequestMessage request)
    {
        apiTester.CurrJwt.Should().NotBeNullOrEmpty("we should be logged in already");
        using var response = await apiTester.HttpClientWithoutRetry.SendAsync(request);
        return new RawResponse(response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Uploads a reset zip with a minimal TUS client (create + single PATCH) without retries, returning the response
    /// of the PATCH, which is where the server processes the zip.
    /// No retries on purpose: the PATCH body can't be replayed, and the frontend (TusUpload.svelte) relies on a
    /// failed upload returning a 500 with an app-error-code, which <see cref="ApiTestBase.HttpClient"/> would hide.
    /// </summary>
    public static async Task<RawResponse> UploadResetZipWithoutRetry(this ApiTestBase apiTester, string projectCode, FileInfo zip)
    {
        apiTester.CurrJwt.Should().NotBeNullOrEmpty("we should be logged in already");
        var bytes = await File.ReadAllBytesAsync(zip.FullName);

        using var create = new HttpRequestMessage(HttpMethod.Post, $"api/project/upload-zip/{projectCode}");
        create.Headers.Add("Tus-Resumable", "1.0.0");
        create.Headers.Add("Upload-Length", bytes.Length.ToString(CultureInfo.InvariantCulture));
        create.Headers.Add("Upload-Metadata", $"filetype {Convert.ToBase64String(Encoding.UTF8.GetBytes("application/zip"))}");
        using var createResponse = await apiTester.HttpClientWithoutRetry.SendAsync(create);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, await createResponse.Content.ReadAsStringAsync());
        var location = createResponse.Headers.Location;
        location.Should().NotBeNull();

        using var patch = new HttpRequestMessage(HttpMethod.Patch, location) { Content = new ByteArrayContent(bytes) };
        patch.Headers.Add("Tus-Resumable", "1.0.0");
        patch.Headers.Add("Upload-Offset", "0");
        patch.Content.Headers.ContentType = new("application/offset+octet-stream");
        return await SendWithoutRetry(apiTester, patch);
    }

    public static string? GetAppErrorCode(this RawResponse response)
    {
        try
        {
            return JsonNode.Parse(response.Body)?["app-error-code"]?.GetValue<string>();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Builds a zip from the entries of <see cref="IntegrationFixture.TemplateRepoZip"/>, renaming or dropping entries,
    /// so tests can cover different zip layouts without checking in more binary fixtures. Callers should delete it when done.
    /// </summary>
    /// <param name="mapEntryName">returns the new entry name, or null to leave the entry out</param>
    /// <param name="extraFiles">additional text files to add, by entry name</param>
    public static FileInfo MakeZipFromTemplateRepo(Func<string, string?> mapEntryName,
        IReadOnlyDictionary<string, string>? extraFiles = null)
    {
        var destPath = NewTempZipPath("repo");
        using (var source = ZipFile.OpenRead(IntegrationFixture.TemplateRepoZip.FullName))
        using (var dest = ZipFile.Open(destPath, ZipArchiveMode.Create))
        {
            foreach (var entry in source.Entries)
            {
                var newName = mapEntryName(entry.FullName);
                if (newName is null) continue;
                var newEntry = dest.CreateEntry(newName);
                if (entry.FullName.EndsWith('/')) continue; // directory entry, no content
                using var from = entry.Open();
                using var to = newEntry.Open();
                from.CopyTo(to);
            }

            foreach (var (name, content) in extraFiles ?? new Dictionary<string, string>())
            {
                using var writer = new StreamWriter(dest.CreateEntry(name).Open());
                writer.Write(content);
            }
        }
        return new FileInfo(destPath);
    }

    /// <summary>
    /// Callers should delete it when done.
    /// </summary>
    public static async Task<FileInfo> SaveTempZip(byte[] zip, string name)
    {
        var file = new FileInfo(NewTempZipPath(name));
        await File.WriteAllBytesAsync(file.FullName, zip);
        return file;
    }

    private static string NewTempZipPath(string name)
    {
        Directory.CreateDirectory(Constants.BasePath);
        return Path.Join(Constants.BasePath, $"{name}-{Guid.NewGuid():N}.zip");
    }
}
