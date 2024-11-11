using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LcmCrdt.Tests;

public class OpenProjectTests
{
    [Fact]
    public async Task OpeningAProjectWorks()
    {
        var sqliteConnectionString = "OpeningAProjectWorks.sqlite";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddLcmCrdtClient();
        using var host = builder.Build();
        var services = host.Services;
        var asyncScope = services.CreateAsyncScope();
        await asyncScope.ServiceProvider.GetRequiredService<ProjectsService>()
            .CreateProject(new(Name: "OpeningAProjectWorks", Path: "", SeedNewProjectData: true));

        var miniLcmApi = (CrdtMiniLcmApi)await asyncScope.ServiceProvider.OpenCrdtProject(new CrdtProject("OpeningAProjectWorks", sqliteConnectionString));
        miniLcmApi.ProjectData.Name.Should().Be("OpeningAProjectWorks");

        await asyncScope.ServiceProvider.GetRequiredService<LcmCrdtDbContext>().Database.EnsureDeletedAsync();
    }
}
