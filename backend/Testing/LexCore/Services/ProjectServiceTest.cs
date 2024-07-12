using LexBoxApi.Services;
using LexBoxApi.Services.Email;
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
    private readonly ProjectWritingSystems _writingSystems = new()
    {
        VernacularWss = new List<FLExWsId>()
        {
            new() { Tag = "en", IsActive = true },
            new() { Tag = "fr", IsActive = false }
        },
        AnalysisWss = new List<FLExWsId>()
        {
            new() { Tag = "en", IsActive = true },
            new() { Tag = "fr", IsActive = false }
        }
    };

    private LexBoxDbContext _lexBoxDbContext;

    public ProjectServiceTest(TestingServicesFixture testing)
    {
        var serviceProvider = testing.ConfigureServices(s =>
        {
            s.AddScoped<IHgService>(_ => Mock.Of<IHgService>(service => service.GetProjectWritingSystems(It.IsAny<ProjectCode>(), It.IsAny<CancellationToken>()) == Task.FromResult(_writingSystems)));
            s.AddScoped<IEmailService>(_ => Mock.Of<IEmailService>());
            s.AddSingleton<IMemoryCache>(_ => Mock.Of<IMemoryCache>());
            s.AddScoped<ProjectService>();
        });
        _projectService = serviceProvider.GetRequiredService<ProjectService>();
        _lexBoxDbContext = serviceProvider.GetRequiredService<LexBoxDbContext>();
    }

    [Fact]
    public async Task CanCreateProject()
    {
        var projectId = await _projectService.CreateProject(
            new(null, "TestProject", "Test", "test", ProjectType.FLEx, RetentionPolicy.Test, false, null, null));
        projectId.ShouldNotBe(default);
    }

    [Fact]
    public async Task CanUpdateProjectLangTags()
    {
        var projectId = await _projectService.CreateProject(
            new(null, "TestProject", "Test", "test", ProjectType.FLEx, RetentionPolicy.Test, false, null, null));
        await _projectService.UpdateProjectLangTags(projectId);
        var project = await _lexBoxDbContext.Projects.Include(p => p.FlexProjectMetadata).SingleAsync(p => p.Id == projectId);
        project.FlexProjectMetadata.ShouldNotBeNull();
        project.FlexProjectMetadata.WritingSystems.ShouldBeEquivalentTo(_writingSystems);
    }

    [Fact]
    public async Task ShouldErrorIfCreatingAProjectWithTheSameCode()
    {
        //first project should be created
        await _projectService.CreateProject(
            new(null, "TestProject", "Test", "test-dup-code", ProjectType.FLEx, RetentionPolicy.Test, false, null, null));

        var exception = await _projectService.CreateProject(
            new(null, "Test2", "Test desc", "test-dup-code", ProjectType.Unknown, RetentionPolicy.Dev, false, null, null)
        ).ShouldThrowAsync<DbUpdateException>();

        exception.InnerException.ShouldBeOfType<PostgresException>()
            .SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
    }
}
