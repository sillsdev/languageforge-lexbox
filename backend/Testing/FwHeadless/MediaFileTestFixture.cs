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
        // HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminJwt);
        ProjectCode = Utils.NewProjectCode();
        ProjectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, ProjectCode, "sena-3");
        // Project folder needs to exist in order for file uploads to be allowed, so trigger first sync including S/R
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, ProjectId);
        await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, ProjectId);
    }

    public async Task DisposeAsync()
    {
        await HttpClient.DeleteAsync($"api/project/{ProjectId}");
    }

    public async Task<Guid> PostFile(string localPath, FileMetadata? metadata = null)
    {
        var filename = Path.GetFileName(localPath);
        using (var formData = new MultipartFormDataContent())
        {
            formData.Add(new StringContent(filename), name: "filename");
            formData.Add(new StringContent(ProjectId.ToString()), name: "projectId");
            if (metadata is not null)
            {
                formData.Add(JsonContent.Create(metadata), name: "metadata");
            }
            var stream = new StreamContent(File.OpenRead(localPath));
            formData.Add(stream, name: "file", fileName: filename);
            // formData.Headers.Add("Authorization", $"Bearer {AdminJwt}"); // Can't do this, not allowed (sigh)
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminJwt); // This sets the default for the entire class, which is not ideal
            var result = await HttpClient.PostAsync($"api/media/", formData);
            var obj = await result.Content.ReadFromJsonAsync<PostFileResult>();
            return obj?.guid ?? Guid.Empty;
        }
    }

    public async Task PutFile(string localPath, Guid fileId)
    {
        // TODO: Implement
    }
}
