using System.Runtime.CompilerServices;
using LexBoxApi.Services;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Testing.Fixtures;

namespace Testing.LexCore.Services;

[Collection(nameof(TestingServicesFixture))]
public class RepoMigrationTests : IAsyncLifetime
{
    private readonly RepoMigrationService _repoMigrationService;
    private readonly Mock<IHgService> _hgServiceMock = new();
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly TaskCompletionSource<Project> _migrateCalled = new();

    public RepoMigrationTests(TestingServicesFixture testing)
    {
        _hgServiceMock.Setup(hg => hg.MigrateRepo(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project project, CancellationToken cancellationToken) =>
            {
                _migrateCalled.SetResult(project);
                return true;
            });
        var serviceProvider = testing.ConfigureServices(s =>
        {
            s.AddSingleton(_hgServiceMock.Object);
            s.AddSingleton<RepoMigrationService>();
        });
        _repoMigrationService = serviceProvider.GetRequiredService<RepoMigrationService>();
        _lexBoxDbContext = serviceProvider.GetDbContext();
    }

    private async Task StartMigrationService()
    {
        await _repoMigrationService.StartAsync(CancellationToken.None);
        //start above doesn't wait for anything. This is a way to ensure that the service has started and queried the db for projects already
        await _repoMigrationService.Started;
    }

    public async Task InitializeAsync()
    {
        await _lexBoxDbContext.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        //need to stop first to avoid race condition with db context
        await _repoMigrationService.StopAsync(CancellationToken.None);
        await _lexBoxDbContext.Database.RollbackTransactionAsync();
    }

    private Project Project([CallerMemberName] string code = "")
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code,
            MigrationStatus = ProjectMigrationStatus.PublicRedmine,
            ProjectOrigin = ProjectMigrationStatus.PublicRedmine,
            RetentionPolicy = RetentionPolicy.Test,
            Type = ProjectType.FLEx,
            Users = new List<ProjectUsers>(),
            LastCommit = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public async Task ProjectsCurrentlyMigratingGetQueuedOnStartup()
    {
        var project = Project();
        project.MigrationStatus = ProjectMigrationStatus.Migrating;
        _lexBoxDbContext.Projects.Add(project);
        await _lexBoxDbContext.SaveChangesAsync();

        await StartMigrationService();
        var actualProject = await ExpectMigrationCalled();

        actualProject.Id.ShouldBe(project.Id);
    }

    [Fact]
    public async Task QueuedProjectsGetMigrated()
    {
        await StartMigrationService();
        var project = Project();
        _lexBoxDbContext.Add(project);
        await _lexBoxDbContext.SaveChangesAsync();

        _repoMigrationService.QueueMigration(project.Code);
        var actualProject = await ExpectMigrationCalled();
        actualProject.Id.ShouldBe(project.Id);
    }

    private async Task<Project> ExpectMigrationCalled()
    {
        (await Task.WhenAny(_migrateCalled.Task, Task.Delay(TimeSpan.FromSeconds(1))))
            .ShouldBe(_migrateCalled.Task, "Migrate not called before 1 second timeout");
        _hgServiceMock.Verify(hg => hg.MigrateRepo(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once());
        var actualProject = await _migrateCalled.Task;
        return actualProject;
    }
}
