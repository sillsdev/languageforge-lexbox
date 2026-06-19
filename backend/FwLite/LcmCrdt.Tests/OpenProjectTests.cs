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
            Path: ""
            ));

        var miniLcmApi = (CrdtMiniLcmApi)await asyncScope.ServiceProvider.OpenCrdtProject(crdtProject);
        miniLcmApi.ProjectData.Name.Should().Be("OpeningAProjectWorks");

        await using var dbContext = await asyncScope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        await dbContext.Database.EnsureDeletedAsync();
    }
}
