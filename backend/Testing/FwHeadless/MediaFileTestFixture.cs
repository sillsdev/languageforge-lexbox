using System.Net.Http.Headers;
using System.Net.Http.Json;
using LexCore.Entities;
using Testing.ApiTests;
using Testing.Services;
using FwHeadless.Models;
using Microsoft.AspNetCore.Http.Extensions;
using FluentAssertions.Execution;

namespace Testing.FwHeadless;

[Trait("Category", "Integration")]
public class MediaFileTestFixture : ApiTestBase, IAsyncLifetime
{
    public Guid ProjectId { get; private set; }
    public string ProjectCode { get; private set; } = "";
    public string LoggedInas { get; private set; } = "";

    public async Task LoginIfNeeded(string username)
    {
        if (CurrJwt is null)
        {
            await LoginAs(username);
            return;
        }
        if (CurrentUser.Username == username) return;
        await LoginAs(username);
        return;
    }

    public async Task InitializeAsync()
    {
        await LoginIfNeeded("admin");
        ProjectCode = Utils.NewProjectCode();
        ProjectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, ProjectCode, "sena-3");
        await Utils.AddMemberToProject(ProjectId, this, "manager", ProjectRole.Manager);
        await Utils.AddMemberToProject(ProjectId, this, "editor", ProjectRole.Editor);
        // Project folder needs to exist in order for file uploads to be allowed, so trigger first sync including S/R
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, ProjectId);
        await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, ProjectId);
    }

    public async Task DisposeAsync()
    {
        // Must ensure we're logged in as admin, since some tests might have changed that
        await LoginIfNeeded("admin");
        await HttpClient.DeleteAsync($"api/project/{ProjectId}");
    }

    public async Task<FileListing?> ListFiles(Guid projectId, string? relativePath = null, string loginAs = "admin")
    {
        await LoginIfNeeded(loginAs);
        var url = $"/api/list-media/{projectId}";
        if (relativePath is not null)
        {
            var qb = new QueryBuilder { { "relativePath", relativePath } };
            url += qb.ToString();
        }
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var result = await HttpClient.SendAsync(request);
        return await result.Content.ReadFromJsonAsync<FileListing>();
    }

    public async Task<Guid> PostFile(string localPath, string? overrideFilename = null, FileMetadata? metadata = null, string loginAs = "admin", bool expectSuccess = true)
    {
        await LoginIfNeeded(loginAs);
        var filename = Path.GetFileName(localPath);
        using (var formData = new MultipartFormDataContent())
        {
            if (overrideFilename is not null)
            {
                formData.Add(new StringContent(overrideFilename), name: "filename");
            }
            formData.Add(new StringContent(ProjectId.ToString()), name: "projectId");
            if (metadata is not null)
            {
                formData.Add(JsonContent.Create(metadata), name: "metadata");
            }
            var stream = new StreamContent(File.OpenRead(localPath));
            formData.Add(stream, name: "file", fileName: filename);
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/media/?projectId={ProjectId}");
            request.Content = formData;
            var result = await HttpClient.SendAsync(request);
            if (expectSuccess) result.EnsureSuccessStatusCode();
            if (!expectSuccess && result.IsSuccessStatusCode) throw new AssertionFailedException($"Expected HTTP call to fail but succeeded instead: {result.StatusCode}");
            var obj = await result.Content.ReadFromJsonAsync<PostFileResult>();
            return obj?.guid ?? Guid.Empty;
        }
    }

    public async Task PutFile(string localPath, Guid fileId, string? overrideFilename = null, FileMetadata? metadata = null, string loginAs = "admin", bool expectSuccess = true)
    {
        await LoginIfNeeded(loginAs);
        var filename = Path.GetFileName(localPath);
        using (var formData = new MultipartFormDataContent())
        {
            if (overrideFilename is not null)
            {
                formData.Add(new StringContent(overrideFilename), name: "filename");
            }
            if (metadata is not null)
            {
                formData.Add(JsonContent.Create(metadata), name: "metadata");
            }
            var stream = new StreamContent(File.OpenRead(localPath));
            formData.Add(stream, name: "file", fileName: filename);
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/media/{fileId}");
            request.Content = formData;
            var result = await HttpClient.SendAsync(request);
            if (expectSuccess) result.EnsureSuccessStatusCode();
            if (!expectSuccess && result.IsSuccessStatusCode) throw new AssertionFailedException($"Expected HTTP call to fail but succeeded instead: {result.StatusCode}");
        }
    }

    public async Task DeleteFile(Guid fileId, string loginAs = "admin", bool expectSuccess = true)
    {
        await LoginIfNeeded(loginAs);
        var request = new HttpRequestMessage(HttpMethod.Delete, $"api/media/{fileId}");
        var result = await HttpClient.SendAsync(request);
        if (expectSuccess) result.EnsureSuccessStatusCode();
        if (!expectSuccess && result.IsSuccessStatusCode) throw new AssertionFailedException($"Expected HTTP call to fail but succeeded instead: {result.StatusCode}");
    }

    public async Task<ApiMetadataEndpointResult?> GetFileMetadata(Guid fileId, string loginAs = "admin", bool expectSuccess = true)
    {
        await LoginIfNeeded(loginAs);
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/metadata/{fileId}");
        var result = await HttpClient.SendAsync(request);
        if (expectSuccess) result.EnsureSuccessStatusCode();
        if (!expectSuccess && result.IsSuccessStatusCode) throw new AssertionFailedException($"Expected HTTP call to fail but succeeded instead: {result.StatusCode}");
        return await result.Content.ReadFromJsonAsync<ApiMetadataEndpointResult>();
    }

    public void CreateDummyFile(string filename, long length)
    {
        using var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        stream.SetLength(length);
        // Yes, that's it!
    }
}
