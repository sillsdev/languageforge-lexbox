using LexBoxApi.Services;
using LexCore.Entities;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;
using Shouldly;
using Testing.Fixtures;

namespace Testing.LexCore.Services;

public class ProjectServiceTest : IClassFixture<TestingServicesFixture>
{
    private readonly ProjectService _projectService;

    public ProjectServiceTest(TestingServicesFixture testing)
    {
        testing.Services.AddScoped<IHgService>(_ => Mock.Of<IHgService>());
        testing.Services.AddScoped<ProjectService>();
        _projectService = testing.ServiceProvider.GetRequiredService<ProjectService>();
    }

    [Fact]
    public async Task CanCreateProject()
    {
        var projectId = await _projectService.CreateProject(
            new(null, "TestProject", "Test", "test", ProjectType.FLEx, RetentionPolicy.Test),
            SeedingData.TestAdminId
        );
        projectId.ShouldNotBe(default);
    }

    [Fact]
    public async Task ShouldErrorIfCreatingAProjectWithTheSameCode()
    {
        //first project should be created
        await _projectService.CreateProject(
            new(null, "TestProject", "Test", "test-dup-code", ProjectType.FLEx, RetentionPolicy.Test),
            SeedingData.TestAdminId
        );

        var exception = await _projectService.CreateProject(
            new(null, "Test2", "Test desc", "test-dup-code", ProjectType.Unknown, RetentionPolicy.Dev),
            SeedingData.TestAdminId
        ).ShouldThrowAsync<DbUpdateException>();

        exception.InnerException.ShouldBeOfType<PostgresException>()
            .SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
    }
}