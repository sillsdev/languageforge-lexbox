using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
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
            //    This is a long-running, non-idempotent operation. The shared tester's HttpClient retries
            //    transient failures/timeouts, so a slow first attempt (which already created the project
            //    row) would be retried and the retry would return 409 "already exists". Use a client that
            //    doesn't retry and allows more time, authenticated as the same admin.
            using var createClient = ApiTestBase.NewHttpClient(_adminApiTester.BaseUrl, retryTransientFailures: false).Client;
            createClient.Timeout = TimeSpan.FromMinutes(5);
            createClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminApiTester.CurrJwt);
            var response = await createClient.PostAsync(
                $"{_adminApiTester.BaseUrl}/api/project/createFromTemplate{query}", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "creation should succeed; body: {0}", await response.Content.ReadAsStringAsync());
            projectId = await response.Content.ReadFromJsonAsync<Guid>();
            projectId.Should().NotBe(Guid.Empty);

            // 2. The first push landed a commit server-side (the empty repo is no longer empty).
            var lastCommit = await _adminApiTester.GetProjectLastCommit(code);
            lastCommit.Should().NotBeNull("the template project should have been pushed to the repo");

            var tagsResponse = await _adminApiTester.HttpClient.GetAsync($"{_adminApiTester.BaseUrl}/hg/{code}/tags?style=json");
            tagsResponse.EnsureSuccessStatusCode();
            var tip = (await tagsResponse.Content.ReadFromJsonAsync<JsonObject>())?["node"]?.ToString();
            tip.Should().NotBeNullOrEmpty();
            tip!.Replace("0", "").Should().NotBeEmpty("the repo tip should not be the all-zero empty-repo hash");

            // 3. The pushed project carries the requested writing systems. Send/Receive split the
            //    template .fwdata into the nested files hg actually tracks; the LangProject's current
            //    analysis/vernacular writing systems live in General/LanguageProject.langproj (XML).
            var langprojResponse = await _adminApiTester.HttpClient.GetAsync(
                $"{_adminApiTester.BaseUrl}/hg/{code}/raw-file/tip/General/LanguageProject.langproj");
            langprojResponse.EnsureSuccessStatusCode();
            var langprojXml = await langprojResponse.Content.ReadAsStringAsync();
            langprojXml.Should().NotBeEmpty();

            // <LanguageProject><LangProject><CurAnalysisWss><Uni>de en pt</Uni></CurAnalysisWss>
            //                               <CurVernWss><Uni>fr es</Uni></CurVernWss> ... </LangProject>
            var langProject = XDocument.Parse(langprojXml).Root?.Element("LangProject");
            langProject.Should().NotBeNull("LanguageProject.langproj should contain a LangProject element");
            var curAnalysisWss = SpaceSeparatedUni(langProject!, "CurAnalysisWss");
            var curVernWss = SpaceSeparatedUni(langProject!, "CurVernWss");

            // The requested writing systems should be current. FieldWorks may add its own defaults (e.g.
            // "en" as an analysis WS), so assert each requested code is present rather than exact equality.
            foreach (var ws in analysis)
                curAnalysisWss.Should().Contain(ws, "analysis writing system {0} should be current in the project", ws);
            foreach (var ws in vernacular)
                curVernWss.Should().Contain(ws, "vernacular writing system {0} should be current in the project", ws);
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

    // Reads a LangProject child element's <Uni> text (a space-separated writing-system list) and splits it.
    private static string[] SpaceSeparatedUni(XElement langProject, string elementName)
    {
        var uni = langProject.Element(elementName)?.Element("Uni")?.Value ?? "";
        return uni.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
