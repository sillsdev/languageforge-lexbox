using LcmCrdt.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static LcmCrdt.CrdtProjectsService;

namespace LcmCrdt.Tests;

public class OpenProjectTests
{
    [Fact]
    public async Task CanCreateExampleProject()
    {
        var sqliteConnectionString = "exampleproject.sqlite";
        if (File.Exists(sqliteConnectionString)) File.Delete(sqliteConnectionString);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        var services = host.Services;
        var asyncScope = services.CreateAsyncScope();
        var crdtProjectsService = asyncScope.ServiceProvider.GetRequiredService<CrdtProjectsService>();
        var crdtProject = await crdtProjectsService.CreateExampleProject("ExampleProject");

        // Opening triggers MigrateDb; the template already carries morph types.
        var api = (CrdtMiniLcmApi)await asyncScope.ServiceProvider.OpenCrdtProject(crdtProject);

        var morphTypes = await api.GetMorphTypes().ToArrayAsync();
        morphTypes.Should().HaveCount(CanonicalMorphTypes.All.Count);

        var writingSystems = await api.GetWritingSystems();
        writingSystems.Vernacular.Select(ws => ws.WsId.Code).Should()
            .Contain(["de", "de-Zxxx-x-audio", "de-fonipa"]);
        writingSystems.Analysis.Select(ws => ws.WsId.Code).Should()
            .Contain(["en", "fr"]);

        var entries = await api.GetEntries().ToArrayAsync();
        entries.Select(e => e.LexemeForm["de"]).Should().Contain(
            ["Apfel", "Banane", "Orange", "Traube", "Beere", "Erdbeere", "Heidelbeere"]);

        var beere = entries.Single(e => e.LexemeForm["de"] == "Beere");
        var complexForms = entries.Where(e => e.Components.Count > 0).ToArray();
        complexForms.Select(e => e.LexemeForm["de"]).Should().Contain(["Erdbeere", "Heidelbeere"]);

        await using var dbContext = await asyncScope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        await dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task ProjectDbIsDeletedIfCreateFails()
    {
        var sqliteConnectionString = "cleaning-up-a-failed-create-works.sqlite";
        if (File.Exists(sqliteConnectionString)) File.Delete(sqliteConnectionString);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        var services = host.Services;
        var asyncScope = services.CreateAsyncScope();
        var crdtProjectsService = asyncScope.ServiceProvider.GetRequiredService<CrdtProjectsService>();
        var exception = new Exception("Test exception");
        var projectRequest = new CreateProjectRequest("CleaningUpAFailedCreateWorks", "cleaning-up-a-failed-create-works", AfterCreate: (_, _) => throw exception);


        var act = async () => await crdtProjectsService.CreateProject(projectRequest);
        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(exception);
        var counter = 0;
        while (File.Exists(sqliteConnectionString) && counter < 10)
        {
            await Task.Delay(1000);
            counter++;
        }

        File.Exists(sqliteConnectionString).Should().BeFalse("database {0} was deleted", sqliteConnectionString);
    }

    [Fact]
    public async Task CreateProjectFromTemplateAppliesRequestedIdentity()
    {
        var code = $"blank-from-template-{Guid.NewGuid():N}";
        var request = new CreateProjectRequest(
            Name: "Blank From Template",
            Code: code,
            Path: "",
            Role: UserProjectRole.Manager);

        await WithProjectFromTemplate(request, "fr", async (services, api) =>
        {
            var miniLcmApi = (CrdtMiniLcmApi)api;
            miniLcmApi.ProjectData.Name.Should().Be("Blank From Template");
            miniLcmApi.ProjectData.Code.Should().Be(code);
            miniLcmApi.ProjectData.Role.Should().Be(UserProjectRole.Manager);
            miniLcmApi.ProjectData.ClientId.Should().NotBe(Guid.Empty);

            var morphTypes = await miniLcmApi.GetMorphTypes().ToArrayAsync();
            morphTypes.Should().HaveCount(CanonicalMorphTypes.All.Count);

            var writingSystems = await miniLcmApi.GetWritingSystems();
            writingSystems.Analysis.Should().ContainSingle().Which.WsId.Should().Be((WritingSystemId)"en");
            writingSystems.Vernacular.Should().ContainSingle().Which.WsId.Should().Be((WritingSystemId)"fr");

            var entry = await miniLcmApi.CreateEntry(new Entry
            {
                LexemeForm = { { "fr", "post-template" } },
            });
            (await miniLcmApi.GetEntry(entry.Id)).Should().NotBeNull();

            await using var dbContext = await services.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
            // The template is imported through the normal MiniLcm write path, so every commit — the imported
            // system data and the post-template writes — is authored under this project's own ClientId.
            var commitClientIds = await dbContext.Set<Commit>().AsNoTracking().Select(c => c.ClientId).Distinct().ToArrayAsync();
            commitClientIds.Should().ContainSingle().Which.Should().Be(miniLcmApi.ProjectData.ClientId);
        });
    }

    [Fact]
    public async Task TemplateCommitsUseTheSystemAuthorAndAreStampedAsTemplateCommits()
    {
        // Create as a signed-in user so the template stamp has a real author to override.
        var request = new CreateProjectRequest("Template Author", $"template-author-{Guid.NewGuid():N}",
            AuthenticatedUser: "Test User", AuthenticatedUserId: "test-user-id", Role: UserProjectRole.Manager);

        await WithProjectFromTemplate(request, "fr", async (services, api) =>
        {
            var historyService = services.GetRequiredService<HistoryService>();

            // The imported template data is re-attributed to System and tagged, overriding the signed-in user.
            var templateCommits = await historyService.ProjectActivity(0, 1000, new ActivityQuery());
            templateCommits.Should().NotBeEmpty();
            templateCommits.Should().AllSatisfy(activity =>
            {
                activity.Metadata.AuthorId.Should().Be(CommitHelpers.SystemAuthorId);
                activity.Metadata.AuthorName.Should().BeNull("the UI owns and translates the System display name");
                activity.Metadata[CommitHelpers.TemplateProp].Should().Be("true");
            });

            // A later edit keeps the signed-in user (not System) — the stamp is scoped to the template import.
            await api.CreateEntry(new() { LexemeForm = { ["fr"] = "post-template" } });
            var latest = (await historyService.ProjectActivity(0, 1, new ActivityQuery())).Single();
            latest.Metadata.AuthorId.Should().Be("test-user-id");
            latest.Metadata.AuthorName.Should().Be("Test User");
            latest.Metadata[CommitHelpers.TemplateProp].Should().BeNull();
        });
    }

    [Fact]
    public async Task CommitMetadataOverrideIsScopedAndRestored()
    {
        var request = new CreateProjectRequest("Commit Metadata Scope", $"commit-metadata-scope-{Guid.NewGuid():N}",
            Role: UserProjectRole.Manager);

        await WithProjectFromTemplate(request, "fr", async (services, api) =>
        {
            var historyService = services.GetRequiredService<HistoryService>();
            var interceptor = services.GetRequiredService<CommitMetadataInterceptor>();

            // A commit made inside the interceptor scope carries the overridden author.
            using (interceptor.Intercept(metadata => metadata.AuthorId = "override-id"))
            {
                await api.CreateEntry(new() { LexemeForm = { ["fr"] = "in scope" } });
            }
            (await LatestCommitAuthorId(historyService)).Should().Be("override-id");

            // Once the scope ends the override is gone.
            await api.CreateEntry(new() { LexemeForm = { ["fr"] = "after scope" } });
            (await LatestCommitAuthorId(historyService)).Should().BeNull();
        });
    }

    private static async Task<string?> LatestCommitAuthorId(HistoryService historyService)
    {
        var latest = await historyService.ProjectActivity(skip: 0, take: 1);
        return latest.Single().Metadata.AuthorId;
    }

    // Spins up a fresh CRDT host, creates a project from the template, opens it, runs the test, and deletes the db.
    private static async Task WithProjectFromTemplate(
        CreateProjectRequest request,
        WritingSystemId vernacularWs,
        Func<IServiceProvider, IMiniLcmApi, Task> test)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var project = await services.GetRequiredService<CrdtProjectsService>().CreateProjectFromTemplate(request, vernacularWs);
        try
        {
            await test(services, await services.OpenCrdtProject(project));
        }
        finally
        {
            await using var dbContext = await services.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
            await dbContext.Database.EnsureDeletedAsync();
        }
    }

    [Theory]
    // Conforms to LexBox's server rule (LexCore.Entities.Project.ProjectCodeRegex): lowercase letters,
    // digits and '-', starting alphanumeric. No uppercase, no underscore. Min length 1.
    [InlineData("foo", true)]
    [InlineData("foo-bar-1", true)]
    [InlineData("a", true)]
    [InlineData("Foo", false)]
    [InlineData("foo_bar", false)]
    [InlineData("-foo", false)]
    [InlineData("", false)]
    public void ProjectCode_ConformsToLexboxRule(string code, bool expected)
    {
        ProjectCode().IsMatch(code).Should().Be(expected);
    }

    [Theory]
    // Regression guard: SanitizeProjectCode must turn raw FwData names like "Sena3" into a code
    // ProjectCode() accepts, otherwise importing such a project throws on the tightened code rule.
    [InlineData("Sena3", "sena3")]
    [InlineData("Sena 3", "sena-3")]
    [InlineData("My_Dictionary", "my-dictionary")]
    [InlineData("-leading", "leading")]
    public void SanitizeProjectCode_ProducesCodeMatchingProjectCodeRule(string name, string expectedCode)
    {
        var code = SanitizeProjectCode(name);
        code.Should().Be(expectedCode);
        ProjectCode().IsMatch(code).Should().BeTrue();
    }

    [Fact]
    public async Task OpeningAProjectWorks()
    {
        var sqliteConnectionString = "opening-a-project-works.sqlite";
        if (File.Exists(sqliteConnectionString)) File.Delete(sqliteConnectionString);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        var services = host.Services;
        var asyncScope = services.CreateAsyncScope();
        var crdtProjectsService = asyncScope.ServiceProvider.GetRequiredService<CrdtProjectsService>();
        var crdtProject = await crdtProjectsService.CreateProject(new(
            Name: "OpeningAProjectWorks",
            Code: "opening-a-project-works",
            Path: ""
            ));

        var miniLcmApi = (CrdtMiniLcmApi)await asyncScope.ServiceProvider.OpenCrdtProject(crdtProject);
        miniLcmApi.ProjectData.Name.Should().Be("OpeningAProjectWorks");

        await using var dbContext = await asyncScope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        await dbContext.Database.EnsureDeletedAsync();
    }
}
