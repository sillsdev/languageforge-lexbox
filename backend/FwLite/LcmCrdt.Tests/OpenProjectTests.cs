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
    public async Task OpeningAProjectWorks()
    {
        var sqliteConnectionString = "OpeningAProjectWorks.sqlite";
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
