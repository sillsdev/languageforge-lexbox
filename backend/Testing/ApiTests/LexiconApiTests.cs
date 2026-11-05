using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using LexCore.Entities;
using LexCore.Sync;
using Testing.Services;

namespace Testing.ApiTests;

/// <summary>
/// A merged sena-3 copy with an Editor ("editor") and an Observer ("manager"). "user" is not a member
/// (and can't be added: its email isn't verified).
/// </summary>
public class LexiconProjectFixture : IAsyncLifetime
{
    public SyncedProjectFixture Project { get; } = new();

    public async Task InitializeAsync()
    {
        await Project.InitializeAsync();
        var admin = new ApiTestBase();
        await admin.LoginAs("admin");
        await Utils.AddMemberToProject(Project.ProjectId, admin, "editor", ProjectRole.Editor);
        await Utils.AddMemberToProject(Project.ProjectId, admin, "manager", ProjectRole.Observer);
    }

    public Task DisposeAsync() => Project.DisposeAsync();
}

[Trait("Category", "Integration")]
[Collection("FwHeadless Sync")]
public class LexiconApiTests(LexiconProjectFixture fixture) : ApiTestBase, IClassFixture<LexiconProjectFixture>
{
    private Guid ProjectId => fixture.Project.ProjectId;
    private string ProjectUrl => $"api/lexicon/project/{ProjectId}";

    private async Task<HttpResponseMessage> CreateEntry()
    {
        var writingSystems = await HttpClient.GetFromJsonAsync<JsonObject>($"{ProjectUrl}/writing-systems");
        var wsId = writingSystems!["vernacular"]![0]!["wsId"]!.GetValue<string>();
        return await HttpClient.PostAsJsonAsync($"{ProjectUrl}/entry",
            new { id = Guid.NewGuid(), lexemeForm = new Dictionary<string, string> { [wsId] = "lexicon-api-test" } });
    }

    [Fact]
    public async Task ListProjects_IncludesTheProjectWithTheCallersRole()
    {
        await LoginAs("editor");
        var projects = await HttpClient.GetFromJsonAsync<JsonArray>("api/lexicon/projects");
        var project = projects!.Single(p => p!["id"]!.GetValue<Guid>() == ProjectId);
        project!["role"]!.GetValue<string>().Should().Be(nameof(ProjectRole.Editor));
    }

    [Fact]
    public async Task Editor_CanCreateReadAndDeleteAnEntry()
    {
        await LoginAs("editor");
        var created = await CreateEntry();
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var entryId = (await created.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<Guid>();

        (await HttpClient.GetAsync($"{ProjectUrl}/entry/{entryId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await HttpClient.DeleteAsync($"{ProjectUrl}/entry/{entryId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await HttpClient.GetAsync($"{ProjectUrl}/entry/{entryId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Observer_CanReadButNotWrite()
    {
        await LoginAs("manager");
        (await HttpClient.GetAsync($"{ProjectUrl}/entries/count")).StatusCode.Should().Be(HttpStatusCode.OK);
        var write = await HttpClient.PostAsJsonAsync($"{ProjectUrl}/entry", new { id = Guid.NewGuid() });
        write.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NonMember_IsForbidden()
    {
        await LoginAs("user");
        (await HttpClient.GetAsync($"{ProjectUrl}/entries/count")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnknownProject_IsNotFound()
    {
        await LoginAs("editor");
        var response = await HttpClient.GetAsync($"api/lexicon/project/{Guid.NewGuid()}/entries/count");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SendAndReceiveToken_IsRefused()
    {
        await LoginAs("admin");
        var tokenResponse = await HttpClient.SendAsync(new(HttpMethod.Get,
            $"api/integration/getProjectToken?projectCode={fixture.Project.ProjectCode}")
        {
            Headers = { Authorization = new("Bearer", CurrJwt) }
        });
        tokenResponse.EnsureSuccessStatusCode();
        var projectToken = (await tokenResponse.Content.ReadFromJsonAsync<JsonObject>())!["projectToken"]!.GetValue<string>();

        var (_, client) = NewHttpClient(BaseUrl, useCookies: false);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{ProjectUrl}/entries/count");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", projectToken);
        var response = await client.SendAsync(request);
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Write_WhenProjectBlocked_IsRefused()
    {
        await LoginAs("admin");
        (await HttpClient.PostAsync($"api/fw-lite/sync/block?projectId={ProjectId}&reason=LexiconApiTests", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        try
        {
            await LoginAs("editor");
            var response = await CreateEntry();
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
            problem!["type"]!.GetValue<string>().Should().Be(LexiconApi.SyncBlockedProblemType);
        }
        finally
        {
            await LoginAs("admin");
            await HttpClient.PostAsync($"api/fw-lite/sync/unblock?projectId={ProjectId}", null);
        }
    }
}
