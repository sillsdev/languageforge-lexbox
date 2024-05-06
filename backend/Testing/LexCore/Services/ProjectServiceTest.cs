using LexBoxApi.Services;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;
using Shouldly;
using Testing.Fixtures;

namespace Testing.LexCore.Services;

[Collection(nameof(TestingServicesFixture))]
public class ProjectServiceTest
{
    private readonly ProjectService _projectService;

    public ProjectServiceTest(TestingServicesFixture testing)
    {
        var serviceProvider = testing.ConfigureServices(s =>
        {
            s.AddScoped<IHgService>(_ => Mock.Of<IHgService>());
            s.AddSingleton<IMemoryCache>(_ => Mock.Of<IMemoryCache>());
            s.AddScoped<ProjectService>();
        });
        _projectService = serviceProvider.GetRequiredService<ProjectService>();
    }

    [Fact]
    public async Task CanCreateProject()
    {
        var projectId = await _projectService.CreateProject(
            new(null, "TestProject", "Test", "test", ProjectType.FLEx, RetentionPolicy.Test, false, null));
        projectId.ShouldNotBe(default);
    }

    [Fact]
    public async Task ShouldErrorIfCreatingAProjectWithTheSameCode()
    {
        //first project should be created
        await _projectService.CreateProject(
            new(null, "TestProject", "Test", "test-dup-code", ProjectType.FLEx, RetentionPolicy.Test, false, null));

        var exception = await _projectService.CreateProject(
            new(null, "Test2", "Test desc", "test-dup-code", ProjectType.Unknown, RetentionPolicy.Dev, false, null)
        ).ShouldThrowAsync<DbUpdateException>();

        exception.InnerException.ShouldBeOfType<PostgresException>()
            .SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
    }
}
