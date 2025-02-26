using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        await crdtProjectsService.CreateProject(new(Name: "OpeningAProjectWorks", Path: "", SeedNewProjectData: true));

        var miniLcmApi = (CrdtMiniLcmApi)await asyncScope.ServiceProvider.OpenCrdtProject(new CrdtProject("OpeningAProjectWorks", sqliteConnectionString));
        miniLcmApi.ProjectData.Name.Should().Be("OpeningAProjectWorks");

        await asyncScope.ServiceProvider.GetRequiredService<LcmCrdtDbContext>().Database.EnsureDeletedAsync();
    }
}
