using System.Net.Http.Headers;
using System.Net.Http.Json;
using LexCore.Entities;
using Testing.ApiTests;
using Testing.Services;
using FwHeadless.Models;

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
        HttpClient.DefaultRequestHeaders.Authorization = null; // OAuth headers will result in 401 Unauthorized response from API
        var result = await HttpClient.DeleteAsync($"api/project/{ProjectId}");
        // result.EnsureSuccessStatusCode(); // Don't cause test failure if deleting project failed somehow
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
            // formData.Headers.Add("Authorization", $"Bearer {jwt}"); // Can't do this, not allowed (sigh)
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt); // This sets the default for the entire class, which is not ideal
            var result = await HttpClient.PostAsync($"api/media/", formData);
            result.EnsureSuccessStatusCode();
            var obj = await result.Content.ReadFromJsonAsync<PostFileResult>();
            return obj?.guid ?? Guid.Empty;
        }
    }

    public async Task PutFile(string localPath, Guid fileId)
    {
        // TODO: Implement
    }
}
