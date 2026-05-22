using LcmCrdt.Objects;
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
        var sqliteConnectionString = "ExampleProject.sqlite";
        if (File.Exists(sqliteConnectionString)) File.Delete(sqliteConnectionString);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        var services = host.Services;
        var asyncScope = services.CreateAsyncScope();
        var crdtProjectsService = asyncScope.ServiceProvider.GetRequiredService<CrdtProjectsService>();
        await crdtProjectsService.CreateExampleProject("ExampleProject");
    }

    [Fact]
    public async Task ProjectDbIsDeletedIfCreateFails()
    {
        var sqliteConnectionString = "CleaningUpAFailedCreateWorks.sqlite";
        if (File.Exists(sqliteConnectionString)) File.Delete(sqliteConnectionString);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        var services = host.Services;
        var asyncScope = services.CreateAsyncScope();
        var crdtProjectsService = asyncScope.ServiceProvider.GetRequiredService<CrdtProjectsService>();
        var exception = new Exception("Test exception");
        var projectRequest = new CreateProjectRequest("CleaningUpAFailedCreateWorks", "CleaningUpAFailedCreateWorks", AfterCreate: (_, _) => throw exception, SeedNewProjectData: true);


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

        // Adding a commit on top of the template forces the chain to extend — and on next open
        // the chain is implicitly validated. If ForceHashChainRebuild left a stale Hash, this
        // step (or the surrounding test's verification of the entry) would surface it.
        var entry = await miniLcmApi.CreateEntry(new Entry
        {
            LexemeForm = { { "fr", "post-template" } },
        });
        (await miniLcmApi.GetEntry(entry.Id)).Should().NotBeNull();

        await using var dbContext = await asyncScope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        await dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task CreateProjectFromTemplate_SubstitutesMorphTypesSeedCommitId()
    {
        var code = $"morph-seed-template-{Guid.NewGuid():N}";
        var sqliteFile = $"{code}.sqlite";
        if (File.Exists(sqliteFile)) File.Delete(sqliteFile);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        var asyncScope = host.Services.CreateAsyncScope();
        var crdtProjectsService = asyncScope.ServiceProvider.GetRequiredService<CrdtProjectsService>();

        var crdtProject = await crdtProjectsService.CreateProjectFromTemplate(new(
            Name: "morph-seed-template",
            Code: code,
            Path: "",
            Role: UserProjectRole.Manager,
            VernacularWs: "en"));
        await asyncScope.ServiceProvider.OpenCrdtProject(crdtProject);

        var expectedSeedId = PreDefinedData.MorphTypesSeedCommitId(crdtProject.Data!.Id);
        await using var dbContext = await asyncScope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        var seedCommitCount = await dbContext.Set<Commit>().AsNoTracking().CountAsync(c => c.Id == expectedSeedId);
        seedCommitCount.Should().Be(1, "the template's seed-commit placeholder must resolve to the project-scoped id that the re-seed gate in CurrentProjectService checks");
        await dbContext.Database.EnsureDeletedAsync();
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
