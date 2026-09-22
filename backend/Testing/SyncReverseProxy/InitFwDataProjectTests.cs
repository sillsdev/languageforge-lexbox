using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using FluentAssertions;
using LexCore.Entities;
using Testing.ApiTests;
using Testing.Fixtures;
using Testing.Services;
using Xunit.Abstractions;
using static Testing.Services.Constants;

namespace Testing.SyncReverseProxy;

/// <summary>
/// End-to-end coverage for the admin "init fwdata project" API. This is the one path that
/// can't be unit-tested: LexBox creates an empty hg repo and FwHeadless does the first push of a
/// template-built .fwdata into it (via LfMergeBridge/Chorus against the real hgweb). Requires the
/// lexbox stack to be up and running, so only run it in CI or with `task test:integration`.
/// </summary>
[Trait("Category", "Integration")]
public class InitFwDataProjectTests : IClassFixture<IntegrationFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly ApiTestBase _adminApiTester;

    public InitFwDataProjectTests(ITestOutputHelper output, IntegrationFixture fixture)
    {
        _output = output;
        _adminApiTester = fixture.AdminApiTester;
    }

    [Fact]
    public async Task InitFwDataProject_PopulatesTheEmptyRepoWithTheRequestedWritingSystems()
    {
        // Valid code (lowercase/digits/hyphen, doesn't start with a hyphen), unique per run.
        var code = $"tmpl-{Guid.NewGuid():N}"[..12];
        var vernacular = new[] { "fr", "es", "ko", "ru", "my" };
        var analysis = new[] { "de", "pt", "th", "id", "he", "el" };
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
            //    doesn't retry and allows more time. Log it in itself (cookie auth) rather than reusing the
            //    shared tester's captured JWT, which may be stale.
            using var createClient = ApiTestBase.NewHttpClient(_adminApiTester.BaseUrl, retryTransientFailures: false).Client;
            createClient.Timeout = TimeSpan.FromMinutes(5);
            await JwtHelper.ExecuteLogin(AdminAuth, includeDefaultScope: true, createClient);
            var response = await createClient.PostAsync(
                $"{_adminApiTester.BaseUrl}/api/project/initFwDataProject{query}", null);
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
            var allAnalysisWss = SpaceSeparatedUni(langProject!, "AnalysisWss");
            var allVernWss = SpaceSeparatedUni(langProject!, "VernWss");
            var curAnalysisWss = SpaceSeparatedUni(langProject!, "CurAnalysisWss");
            var curVernWss = SpaceSeparatedUni(langProject!, "CurVernWss");

            // The requested writing systems should be current. FieldWorks may add its own defaults (e.g.
            // "en" as an analysis WS), so assert each requested code is present rather than exact equality.
            foreach (var ws in analysis)
                curAnalysisWss.Should().Contain(ws, "analysis writing system {0} should be current in the project", ws);
            foreach (var ws in vernacular)
                curVernWss.Should().Contain(ws, "vernacular writing system {0} should be current in the project", ws);

            // The requested writing systems should *also* be in the correct order. Any writing systems added
            // by FieldWorks should come last.
            allAnalysisWss.Take(analysis.Length).Should().Equal(analysis, "analysis writing systems were in the wrong order");
            allVernWss.Take(vernacular.Length).Should().Equal(vernacular, "vernacular writing systems were in the wrong order");
            curAnalysisWss.Should().Equal(analysis, "current analysis writing systems should not contain any extras");
            curVernWss.Should().Equal(vernacular, "current vernacular writing systems should not contain any extras");
        }
        finally
        {
            if (projectId != default) await SoftDeleteProject(projectId);
        }
    }

    /// <summary>
    /// Only admins may say where a project came from, so a non-admin passing <c>projectOrigin</c>
    /// must be rejected. Today it's [AdminRequired] on the endpoint that rejects it (403); once this
    /// endpoint opens up to non-admins and that attribute goes away, the controller's own
    /// AssertIsAdmin() will reject it (401). Either way the request must fail and no project may be
    /// created, which is what this test pins down across that change.
    /// To check it isn't passing for the wrong reason, temporarily delete [AdminRequired] from
    /// ProjectController.InitFwDataProject, rebuild, and re-run: it must still pass.
    /// </summary>
    [Fact]
    public async Task InitFwDataProject_RejectsProjectOriginFromANonAdmin()
    {
        var managerApiTester = new ApiTestBase();
        await managerApiTester.LoginAs(ManagerAuth.Username, ManagerAuth.Password);

        // Everything except projectOrigin is valid, so a 400 from input validation can't be what
        // rejects this, and the origin itself is a real ProjectMigrationStatus name.
        var code = $"tmpl-{Guid.NewGuid():N}"[..12];
        var response = await managerApiTester.HttpClient.PostAsync(
            $"{managerApiTester.BaseUrl}/api/project/initFwDataProject" +
            $"?code={code}&wsVernacular=fr&projectOrigin={nameof(ProjectMigrationStatus.LanguageForgeNonSR)}", null);

        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            // In developer mode, a thrown System.UnauthorizedAccessException exception results in an HTTP 500
            // instead of a 4xx code, with a JSON structure that includes `"title":"System.UnauthorizedAccessException"`.
            // We check for that special case here; any non-JSON or JSON that has a different title would indicate a
            // different problem and be a test failure.
            JsonNode? problemDetails = null;
            try
            {
                var body = await response.Content.ReadAsStringAsync();
                problemDetails = JsonNode.Parse(body);
            }
            catch (JsonException)
            {
                // Do nothing here, the Should().NotBeNull() check below handles this
            }
            var failureReasonMsg = "HTTP 500 should have been caused by developer page printing "
                + "a traceback due to " + nameof(UnauthorizedAccessException);
            problemDetails.Should().NotBeNull(failureReasonMsg);
            problemDetails?["title"]?.ToString().Should().EndWithEquivalentOf(
                nameof(UnauthorizedAccessException), failureReasonMsg);
        }
        else
        {
            response.StatusCode.Should().BeOneOf([HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized],
                "a non-admin may not set projectOrigin; body: {0}", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task InitFwDataProject_RejectsWhenNoVernacularWritingSystem()
    {
        var code = $"tmpl-{Guid.NewGuid():N}"[..12];
        var response = await _adminApiTester.HttpClient.PostAsync(
            $"{_adminApiTester.BaseUrl}/api/project/initFwDataProject?code={code}&wsAnalysis=en", null);
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
            _output.WriteLine($"[InitFwDataProjectTests] Ignored cleanup exception: {ex}");
        }
    }
}
