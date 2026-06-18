using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SIL.Harmony;
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

        // Opening triggers MigrateDb; the template already carries morph types so no redundant seed runs.
        var api = (CrdtMiniLcmApi)await asyncScope.ServiceProvider.OpenCrdtProject(crdtProject);

        var morphTypes = await api.GetMorphTypes().ToArrayAsync();
        morphTypes.Should().HaveCount(CanonicalMorphTypes.All.Count);

        var writingSystems = await api.GetWritingSystems();
        writingSystems.Vernacular.Select(ws => ws.WsId.Code).Should()
            .Equal("de", "de-Zxxx-x-audio", "de-fonipa");
        writingSystems.Analysis.Select(ws => ws.WsId.Code).Should().Equal("en", "fr");
        // The template-provided "de"/"en" were updated in place to the demo's presentation, not duplicated.
        writingSystems.Vernacular.Single(ws => ws.WsId == "de").Name.Should().Be("German");
        writingSystems.Analysis.Single(ws => ws.WsId == "en").Name.Should().Be("English");

        var entries = await api.GetEntries().ToArrayAsync();
        entries.Select(e => e.LexemeForm["de"]).Should().BeEquivalentTo(
            "Apfel", "Banane", "Orange", "Traube", "Beere", "Erdbeere", "Heidelbeere");

        var beere = entries.Single(e => e.LexemeForm["de"] == "Beere");
        var complexForms = entries.Where(e => e.Components.Count > 0).ToArray();
        complexForms.Select(e => e.LexemeForm["de"]).Should().BeEquivalentTo("Erdbeere", "Heidelbeere");
        complexForms.Should().AllSatisfy(cf =>
        {
            cf.Components.Should().ContainSingle().Which.ComponentEntryId.Should().Be(beere.Id);
            cf.ComplexFormTypes.Should().ContainSingle().Which.Name["en"].Should().Be("Compound");
        });

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
        var projectRequest = new CreateProjectRequest("CleaningUpAFailedCreateWorks", "cleaning-up-a-failed-create-works", AfterCreate: (_, _) => throw exception, SeedNewProjectData: true);


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
        var sqliteFile = $"{code}.sqlite";
        if (File.Exists(sqliteFile)) File.Delete(sqliteFile);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        var asyncScope = host.Services.CreateAsyncScope();
        var crdtProjectsService = asyncScope.ServiceProvider.GetRequiredService<CrdtProjectsService>();

        var crdtProject = await crdtProjectsService.CreateProjectFromTemplate(new(
            Name: "Blank From Template",
            Code: code,
            Path: "",
            Role: UserProjectRole.Manager,
            VernacularWs: "fr"));

        var miniLcmApi = (CrdtMiniLcmApi)await asyncScope.ServiceProvider.OpenCrdtProject(crdtProject);
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

        await using var dbContext = await asyncScope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        // The template is imported through the normal MiniLcm write path, so every commit — the imported
        // system data and the post-template writes — is authored under this project's own ClientId.
        var commitClientIds = await dbContext.Set<Commit>().AsNoTracking().Select(c => c.ClientId).Distinct().ToArrayAsync();
        commitClientIds.Should().ContainSingle().Which.Should().Be(miniLcmApi.ProjectData.ClientId);
        await dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task TemplatedProject_SyncsCleanlyToBlankPeer()
    {
        var sourceCode = $"sync-src-{Guid.NewGuid():N}";
        var targetCode = $"sync-dst-{Guid.NewGuid():N}";
        foreach (var c in new[] { sourceCode, targetCode })
            if (File.Exists($"{c}.sqlite")) File.Delete($"{c}.sqlite");

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        var sourceScope = host.Services.CreateAsyncScope();
        var targetScope = host.Services.CreateAsyncScope();

        var sourceProject = await sourceScope.ServiceProvider.GetRequiredService<CrdtProjectsService>()
            .CreateProjectFromTemplate(new(
                Name: "Sync Source",
                Code: sourceCode,
                Path: "",
                Role: UserProjectRole.Manager,
                VernacularWs: "fr"));
        var targetProject = await targetScope.ServiceProvider.GetRequiredService<CrdtProjectsService>()
            .CreateProject(new(
                Name: "Sync Target",
                Code: targetCode,
                Path: "",
                SeedNewProjectData: false));

        var sourceApi = (CrdtMiniLcmApi)await sourceScope.ServiceProvider.OpenCrdtProject(sourceProject);
        var targetApi = (CrdtMiniLcmApi)await targetScope.ServiceProvider.OpenCrdtProject(targetProject);

        // Exercises the full Harmony sync protocol against the templated project's chain — the import's
        // commits must form a valid chain that AddRangeFromSync's hash validation accepts.
        await targetScope.ServiceProvider.GetRequiredService<DataModel>()
            .SyncWith(sourceScope.ServiceProvider.GetRequiredService<DataModel>());

        var morphTypes = await targetApi.GetMorphTypes().ToArrayAsync();
        morphTypes.Should().HaveCount(CanonicalMorphTypes.All.Count);
        var writingSystems = await targetApi.GetWritingSystems();
        writingSystems.Vernacular.Should().ContainSingle().Which.WsId.Should().Be((WritingSystemId)"fr");

        await using (var db = await sourceScope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync())
            await db.Database.EnsureDeletedAsync();
        await using (var db = await targetScope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync())
            await db.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task TemplatedProjects_HaveDisjointCommitIds()
    {
        // Two projects derived from the same template must not share commit Ids. The template is imported
        // through the normal MiniLcm write path, so each project mints its own fresh commits — disjoint by
        // construction. Guards against a regression that reintroduced fixed/derived commit Ids into the
        // snapshot or importer.
        var codeA = $"disjoint-a-{Guid.NewGuid():N}";
        var codeB = $"disjoint-b-{Guid.NewGuid():N}";
        foreach (var c in new[] { codeA, codeB })
            if (File.Exists($"{c}.sqlite")) File.Delete($"{c}.sqlite");

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();

        async Task<HashSet<Guid>> CommitIdsFor(string code)
        {
            await using var scope = host.Services.CreateAsyncScope();
            var crdtProjectsService = scope.ServiceProvider.GetRequiredService<CrdtProjectsService>();
            var project = await crdtProjectsService.CreateProjectFromTemplate(new(
                Name: code, Code: code, Path: "",
                Role: UserProjectRole.Manager, VernacularWs: "fr"));
            await scope.ServiceProvider.OpenCrdtProject(project);
            await using var db = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
            var ids = await db.Set<Commit>().AsNoTracking().Select(c => c.Id).ToHashSetAsync();
            await db.Database.EnsureDeletedAsync();
            return ids;
        }

        var commitsA = await CommitIdsFor(codeA);
        var commitsB = await CommitIdsFor(codeB);

        commitsA.Should().NotBeEmpty();
        commitsA.Intersect(commitsB).Should().BeEmpty(
            "two templated projects must not share any commit Ids (each mints its own during import)");
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
            Path: "",
            SeedNewProjectData: true
            ));

        var miniLcmApi = (CrdtMiniLcmApi)await asyncScope.ServiceProvider.OpenCrdtProject(crdtProject);
        miniLcmApi.ProjectData.Name.Should().Be("OpeningAProjectWorks");

        await using var dbContext = await asyncScope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        await dbContext.Database.EnsureDeletedAsync();
    }
}
