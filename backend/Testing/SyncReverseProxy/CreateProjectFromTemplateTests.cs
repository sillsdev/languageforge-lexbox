using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Testing.ApiTests;
using Testing.Fixtures;
using Xunit.Abstractions;

namespace Testing.SyncReverseProxy;

/// <summary>
/// End-to-end coverage for the admin "create project from a template" API. This is the one path that
/// can't be unit-tested: LexBox creates an empty hg repo and FwHeadless does the first push of a
/// template-built .fwdata into it (via LfMergeBridge/Chorus against the real hgweb). Requires the
/// lexbox stack, so it only runs in CI.
/// </summary>
[Trait("Category", "Integration")]
public class CreateProjectFromTemplateTests : IClassFixture<IntegrationFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly ApiTestBase _adminApiTester;

    public CreateProjectFromTemplateTests(ITestOutputHelper output, IntegrationFixture fixture)
    {
        _output = output;
        _adminApiTester = fixture.AdminApiTester;
    }

    [Fact]
    public async Task CreateFromTemplate_PopulatesTheEmptyRepoWithTheRequestedWritingSystems()
    {
        // Valid code (lowercase/digits/hyphen, doesn't start with a hyphen), unique per run.
        var code = $"tmpl-{Guid.NewGuid():N}"[..12];
        var vernacular = new[] { "fr", "es" };
        var analysis = new[] { "de", "pt" };
        var query = $"?code={code}"
                    + string.Concat(vernacular.Select(ws => $"&wsVernacular={ws}"))
                    + string.Concat(analysis.Select(ws => $"&wsAnalysis={ws}"));

        Guid projectId = default;
        try
        {
            // 1. Create the project via the admin endpoint (creates the DB row + empty repo, then has
            //    FwHeadless build the template .fwdata and push it into the empty repo).
            var response = await _adminApiTester.HttpClient.PostAsync(
                $"{_adminApiTester.BaseUrl}/api/project/createFromTemplate{query}", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "creation should succeed; body: {0}", await response.Content.ReadAsStringAsync());
            projectId = await response.Content.ReadFromJsonAsync<Guid>();
            projectId.Should().NotBe(Guid.Empty);

            // 2. The first push landed a commit server-side (the empty repo is no longer empty).
            var lastCommit = await _adminApiTester.GetProjectLastCommit(code);
            lastCommit.Should().NotBeNull("the template project should have been pushed to the repo");

            var tagsResponse = await _adminApiTester.HttpClient.GetAsync($"/hg/{code}/tags?style=json");
            tagsResponse.EnsureSuccessStatusCode();
            var tip = (await tagsResponse.Content.ReadFromJsonAsync<JsonObject>())?["node"]?.ToString();
            tip.Should().NotBeNullOrEmpty();
            tip!.Replace("0", "").Should().NotBeEmpty("the repo tip should not be the all-zero empty-repo hash");

            // 3. The pushed .fwdata carries the requested writing systems. FwHeadless always names the
            //    file fw.fwdata (its fixed project name), so fetch that from hgweb and check the codes.
            var fwDataResponse = await _adminApiTester.HttpClient.GetAsync($"/hg/{code}/raw-file/tip/fw.fwdata");
            fwDataResponse.EnsureSuccessStatusCode();
            var fwData = await fwDataResponse.Content.ReadAsStringAsync();
            fwData.Should().NotBeEmpty();
            foreach (var ws in vernacular.Concat(analysis))
                fwData.Should().Contain($"\"{ws}\"", "writing system {0} should be present in the pushed project", ws);
        }
        finally
        {
            if (projectId != default) await SoftDeleteProject(projectId);
        }
    }

    [Fact]
    public async Task CreateFromTemplate_RejectsWhenNoVernacularWritingSystem()
    {
        var code = $"tmpl-{Guid.NewGuid():N}"[..12];
        var response = await _adminApiTester.HttpClient.PostAsync(
            $"{_adminApiTester.BaseUrl}/api/project/createFromTemplate?code={code}&wsAnalysis=en", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task SoftDeleteProject(Guid projectId)
    {
        try
        {
            await _adminApiTester.ExecuteGql($$"""
                mutation {
                  softDeleteProject(input: { projectId: "{{projectId}}" }) {
                    project { id }
                    errors { __typename }
                  }
                }
                """);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"[CreateProjectFromTemplateTests] Ignored cleanup exception: {ex}");
        }
    }
}
