using System.Net.Http.Headers;
using System.Net.Http.Json;
using LexCore.Entities;
using Testing.ApiTests;
using Testing.Services;
using FwHeadless.Models;
using Microsoft.AspNetCore.Http.Extensions;

namespace Testing.FwHeadless;

[Trait("Category", "Integration")]
public class MediaFileTestFixture : ApiTestBase, IAsyncLifetime
{
    public Guid ProjectId { get; private set; }
    public string ProjectCode { get; private set; } = "";
    public string AdminJwt { get; private set; } = "";

    public async Task InitializeAsync()
    {
        AdminJwt = await LoginAs("admin");
        ProjectCode = Utils.NewProjectCode();
        ProjectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, ProjectCode, "sena-3");
        // Project folder needs to exist in order for file uploads to be allowed, so trigger first sync including S/R
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, ProjectId);
        await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, ProjectId);
    }

    public async Task DisposeAsync()
    {
        // Must ensure we're logged in as admin, since some tests might have changed that
        await LoginAs("admin");
        await HttpClient.DeleteAsync($"api/project/{ProjectId}");
    }

    public async Task<FileListing?> ListFiles(Guid projectId, string? relativePath = null, string? loginAs = null)
    {
        var jwt = AdminJwt;
        if (loginAs is not null)
        {
            jwt = await LoginAs(loginAs);
        }
        var url = $"/api/list-media/{projectId}";
        if (relativePath is not null)
        {
            var qb = new QueryBuilder { { "relativePath", relativePath } };
            url += qb.ToString();
        }
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var result = await HttpClient.SendAsync(request);
        return await result.Content.ReadFromJsonAsync<FileListing>();
    }

    public async Task<Guid> PostFile(string localPath, FileMetadata? metadata = null, string? loginAs = null)
    {
        var filename = Path.GetFileName(localPath);
        using (var formData = new MultipartFormDataContent())
        {
            var jwt = AdminJwt;
            if (loginAs is not null)
            {
                jwt = await LoginAs(loginAs);
            }
            formData.Add(new StringContent(filename), name: "filename");
            formData.Add(new StringContent(ProjectId.ToString()), name: "projectId");
            if (metadata is not null)
            {
                formData.Add(JsonContent.Create(metadata), name: "metadata");
            }
            var stream = new StreamContent(File.OpenRead(localPath));
            formData.Add(stream, name: "file", fileName: filename);
            var request = new HttpRequestMessage(HttpMethod.Post, "api/media/");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = formData;
            var result = await HttpClient.SendAsync(request);
            result.EnsureSuccessStatusCode();
            var obj = await result.Content.ReadFromJsonAsync<PostFileResult>();
            return obj?.guid ?? Guid.Empty;
        }
    }

    public async Task PutFile(string localPath, Guid fileId, FileMetadata? metadata = null, string? loginAs = null)
    {
        var filename = Path.GetFileName(localPath);
        using (var formData = new MultipartFormDataContent())
        {
            var jwt = AdminJwt;
            if (loginAs is not null)
            {
                jwt = await LoginAs(loginAs);
            }
            formData.Add(new StringContent(filename), name: "filename");
            // formData.Add(new StringContent(fileId.ToString()), name: "fileId"); // TODO: Check if required
            // formData.Add(new StringContent(ProjectId.ToString()), name: "projectId"); // TODO: Should not be required
            if (metadata is not null)
            {
                formData.Add(JsonContent.Create(metadata), name: "metadata");
            }
            var stream = new StreamContent(File.OpenRead(localPath));
            formData.Add(stream, name: "file", fileName: filename);
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/media/{fileId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = formData;
            var result = await HttpClient.SendAsync(request);
            result.EnsureSuccessStatusCode();
        }
    }

    public async Task DeleteFile(Guid fileId, string? loginAs = null)
    {
        var jwt = AdminJwt;
        if (loginAs is not null)
        {
            jwt = await LoginAs(loginAs);
        }
        var request = new HttpRequestMessage(HttpMethod.Delete, $"api/media/{fileId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var result = await HttpClient.SendAsync(request);
        result.EnsureSuccessStatusCode();
    }
}
