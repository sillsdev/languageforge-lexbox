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
using FluentAssertions;
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
            new() { Tag = "en", IsActive = true, IsDefault = true },
            new() { Tag = "fr", IsActive = false, IsDefault = false }
        },
        AnalysisWss = new List<FLExWsId>()
        {
            new() { Tag = "en", IsActive = true, IsDefault = true },
            new() { Tag = "fr", IsActive = false, IsDefault = false }
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
            new(null, "TestProject", "Test", "test1", ProjectType.FLEx, RetentionPolicy.Test, false, null, null));
        projectId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CanUpdateProjectLangTags()
    {
        var projectId = await _projectService.CreateProject(
            new(null, "TestProject", "Test", "test2", ProjectType.FLEx, RetentionPolicy.Test, false, null, null));
        await _projectService.UpdateProjectLangTags(projectId);
        var project = await _lexBoxDbContext.Projects.Include(p => p.FlexProjectMetadata).SingleAsync(p => p.Id == projectId);
        project.FlexProjectMetadata.ShouldNotBeNull();
        project.FlexProjectMetadata.WritingSystems.Should().BeEquivalentTo(_writingSystems);
    }

    [Fact]
    public async Task ShouldErrorIfCreatingAProjectWithTheSameCode()
    {
        //first project should be created
        await _projectService.CreateProject(
            new(null, "TestProject", "Test", "test-dup-code", ProjectType.FLEx, RetentionPolicy.Test, false, null, null));

        var act = () => _projectService.CreateProject(
            new(null, "Test2", "Test desc", "test-dup-code", ProjectType.Unknown, RetentionPolicy.Dev, false, null, null)
        );

        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }
}
