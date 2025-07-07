using System.Net.Http.Json;
using LexCore.Entities;
using Testing.ApiTests;
using Testing.Services;
using FwHeadless.Models;
using Microsoft.AspNetCore.Http.Extensions;
using System.Net.Http.Headers;

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

    public async Task<(FileListing?, HttpResponseMessage)> ListFiles(Guid projectId, string? relativePath = null, string loginAs = "admin")
    {
        await LoginIfNeeded(loginAs);
        var url = $"/api/media/list/{projectId}";
        if (relativePath is not null)
        {
            var qb = new QueryBuilder { { "relativePath", relativePath } };
            url += qb.ToString();
        }
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var result = await HttpClient.SendAsync(request);
        return (await result.Content.ReadFromJsonAsync<FileListing>(), result);
    }

    public async Task<(Guid, HttpResponseMessage)> PostFile(string localPath, string? overrideFilename = null, string? overrideSubfolder = null, string? contentType = null, bool deleteContentLengthHeader = false, FileMetadata? metadata = null, string loginAs = "admin", IDictionary<string, string>? extraFields = null)
    {
        await LoginIfNeeded(loginAs);
        var filename = Path.GetFileName(localPath);
        using (var formData = new MultipartFormDataContent())
        {
            if (overrideFilename is not null)
            {
                formData.Add(new StringContent(overrideFilename), name: "filename");
            }
            if (overrideSubfolder is not null)
            {
                formData.Add(new StringContent(overrideSubfolder), name: "linkedFilesSubfolderOverride");
            }
            if (metadata is not null)
            {
                if (metadata.Author is not null)
                {
                    formData.Add(new StringContent(metadata.Author), name: "author");
                }
                if (metadata.License is not null)
                {
                    formData.Add(new StringContent(metadata.License.Value.ToString()), name: "license");
                }
                if (extraFields is not null)
                {
                    foreach (var kv in extraFields)
                    {
                        formData.Add(new StringContent(kv.Value), name: kv.Key);
                    }
                }
            }
            var stream = new StreamContent(File.OpenRead(localPath));
            if (contentType is not null) stream.Headers.ContentType = new(contentType);
            formData.Add(stream, name: "file", fileName: filename);
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/media/?projectId={ProjectId}");
            request.Content = formData;
            if (deleteContentLengthHeader) request.Content.Headers.ContentLength = null;
            var result = await HttpClient.SendAsync(request);
            var obj = await result.Content.ReadFromJsonAsync<PostFileResult>();
            return (obj?.guid ?? Guid.Empty, result);
        }
    }

    public async Task<HttpResponseMessage> PutFile(string localPath, Guid fileId, string? overrideFilename = null, string? overrideSubfolder = null, string? contentType = null, bool deleteContentLengthHeader = false, FileMetadata? metadata = null, string loginAs = "admin")
    {
        await LoginIfNeeded(loginAs);
        var filename = Path.GetFileName(localPath);
        using (var formData = new MultipartFormDataContent())
        {
            if (overrideFilename is not null)
            {
                formData.Add(new StringContent(overrideFilename), name: "filename");
            }
            if (overrideSubfolder is not null)
            {
                formData.Add(new StringContent(overrideSubfolder), name: "linkedFilesSubfolderOverride");
            }
            if (metadata is not null)
            {
                formData.Add(JsonContent.Create(metadata), name: "metadata");
            }
            formData.Add(new StringContent(ProjectId.ToString()), name: "projectId");
            var stream = new StreamContent(File.OpenRead(localPath));
            if (contentType is not null) stream.Headers.ContentType = new(contentType);
            formData.Add(stream, name: "file", fileName: filename);
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/media/{fileId}");
            request.Content = formData;
            if (deleteContentLengthHeader) request.Content.Headers.ContentLength = null;
            var result = await HttpClient.SendAsync(request);
            return result;
        }
    }

    public async Task<HttpResponseMessage> DeleteFile(Guid fileId, string loginAs = "admin")
    {
        await LoginIfNeeded(loginAs);
        var request = new HttpRequestMessage(HttpMethod.Delete, $"api/media/{fileId}");
        var result = await HttpClient.SendAsync(request);
        return result;
    }

    public async Task<HttpResponseMessage> DownloadFile(Guid fileId, string loginAs = "admin", string? hash = null, int startAt = 0, int endAt = 0)
    {
        await LoginIfNeeded(loginAs);
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/media/{fileId}");
        if (hash is not null)
        {
            if (!hash.StartsWith('"')) hash = "\"" + hash;
            if (!hash.EndsWith('"')) hash = hash + "\"";
            var header = new EntityTagHeaderValue(hash, isWeak: false);
            request.Headers.IfNoneMatch.Add(header);
        }
        if (startAt > 0)
        {
            request.Headers.Range = new(startAt, endAt > 0 ? endAt : null);
        }
        var result = await HttpClient.SendAsync(request);
        return result;
    }

    public async Task<(ApiMetadataEndpointResult?, HttpResponseMessage)> GetFileMetadata(Guid fileId, string loginAs = "admin")
    {
        await LoginIfNeeded(loginAs);
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/media/metadata/{fileId}");
        var result = await HttpClient.SendAsync(request);
        return (await result.Content.ReadFromJsonAsync<ApiMetadataEndpointResult>(), result);
    }

    public void CreateDummyFile(string filename, long length)
    {
        using var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        stream.SetLength(length);
        // Yes, that's it!
    }
}
