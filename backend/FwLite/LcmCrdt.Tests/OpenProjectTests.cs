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
        var projectRequest = new CreateProjectRequest("CleaningUpAFailedCreateWorks", AfterCreate: (_, _) => throw new Exception("Test exception"), SeedNewProjectData: true);

        try
        {
            await crdtProjectsService.CreateProject(projectRequest);
            Assert.Fail("Create should fail");
        }
        catch
        {
            var counter = 0;
            while (File.Exists(sqliteConnectionString) && counter < 10)
            {
                await Task.Delay(1000);
                counter++;
            }
            File.Exists(sqliteConnectionString).Should().BeFalse();
        }
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
        var crdtProject = await crdtProjectsService.CreateProject(new(Name: "OpeningAProjectWorks", Path: "", SeedNewProjectData: true));

        var miniLcmApi = (CrdtMiniLcmApi)await asyncScope.ServiceProvider.OpenCrdtProject(crdtProject);
        miniLcmApi.ProjectData.Name.Should().Be("OpeningAProjectWorks");

        await asyncScope.ServiceProvider.GetRequiredService<LcmCrdtDbContext>().Database.EnsureDeletedAsync();
    }
}
