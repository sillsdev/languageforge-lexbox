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
    private readonly Mock<ILexProxyService> _lexProxyServiceMock = new();
    private readonly CancellationTokenSource _migrationServiceToken = new();
    private readonly Mock<IHgService> _hgServiceMock = new();
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly TaskCompletionSource<Project> _migrateCalled =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _autoCompleteMigration = true;
    private readonly TaskCompletionSource<bool> _migrationCompleted = new();

    public RepoMigrationTests(TestingServicesFixture testing)
    {
        RepoMigrationService.QueueAfterReadBlockDelay = TimeSpan.Zero;
        _hgServiceMock.Setup(hg => hg.MigrateRepo(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns((Project project, CancellationToken cancellationToken) =>
            {
                _migrateCalled.SetResult(project);
                if (_autoCompleteMigration)
                {
                    return Task.FromResult(true);
                }

                return _migrationCompleted.Task;
            });
        var serviceProvider = testing.ConfigureServices(s =>
        {
            s.AddSingleton(_hgServiceMock.Object);
            s.AddSingleton<RepoMigrationService>();
            s.AddSingleton(_lexProxyServiceMock.Object);
        });
        _repoMigrationService = serviceProvider.GetRequiredService<RepoMigrationService>();
        _lexBoxDbContext = serviceProvider.GetDbContext();
    }

    private async Task StartMigrationService()
    {
        await _repoMigrationService.StartAsync(_migrationServiceToken.Token);
        //start above doesn't wait for anything. This is a way to ensure that the service has started and queried the db for projects already
        await _repoMigrationService.Started;
    }

    public async Task InitializeAsync()
    {
        await _lexBoxDbContext.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        _migrationServiceToken.Cancel();
        if (!_migrationCompleted.Task.IsCompleted)
        {
            //service stop will deadlock if this task isn't cancelled.
            _migrationCompleted.SetCanceled();
        }

        //need to stop first to avoid race condition with db context, if we don't pass in a token this could hang the tests
        await _repoMigrationService.StopAsync(new CancellationTokenSource(10).Token);
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

    private async Task ExpectMigrationNotCalled()
    {
        (await Task.WhenAny(_migrateCalled.Task, Task.Delay(TimeSpan.FromSeconds(1)))).ShouldNotBe(_migrateCalled.Task);
    }

    [Fact]
    public async Task DontMigrateWithSendReceiveInFlight()
    {
        await StartMigrationService();
        var project = Project();
        _lexBoxDbContext.Add(project);
        await _lexBoxDbContext.SaveChangesAsync();

        using (await _repoMigrationService.BeginSendReceive(project.Code))
        {
            _repoMigrationService.QueueMigration(project.Code);
            await ExpectMigrationNotCalled();
        }

        await ExpectMigrationCalled();
    }

    [Fact]
    public async Task CanNotSendReceiveWhenMigrating()
    {
        await StartMigrationService();
        var project = Project();
        _lexBoxDbContext.Add(project);
        await _lexBoxDbContext.SaveChangesAsync();

        _autoCompleteMigration = false;
        _repoMigrationService.QueueMigration(project.Code);
        await ExpectMigrationCalled();

        //not allowed to begin
        var sendReceiveToken = await _repoMigrationService.BeginSendReceive(project.Code);
        sendReceiveToken.ShouldBeNull();

        //migration finished
        _migrationCompleted.SetResult(true);
        await _repoMigrationService.MigrationCompleted.ReadAsync();
        _lexProxyServiceMock.Verify(l => l.ClearProjectMigrationInfo(project.Code));

        //allowed to begin now
        sendReceiveToken = await _repoMigrationService.BeginSendReceive(project.Code);
        sendReceiveToken.ShouldNotBeNull();
        sendReceiveToken.Dispose();
    }

    [Fact]
    public async Task MigratingOneProjectDoesNotBlockAnother()
    {
        await StartMigrationService();
        var projectA = Project("project-a");
        var projectB = Project("project-b");
        _lexBoxDbContext.Add(projectA);
        _lexBoxDbContext.Add(projectB);
        await _lexBoxDbContext.SaveChangesAsync();
        _autoCompleteMigration = false;

        _repoMigrationService.QueueMigration(projectA.Code);
        await ExpectMigrationCalled();

        var srToken = await _repoMigrationService.BeginSendReceive(projectB.Code);
        srToken.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendReceiveForOneProjectDoesNotBlockMigrationOfAnother()
    {
        await StartMigrationService();
        var projectA = Project("project-a");
        var projectB = Project("project-b");
        _lexBoxDbContext.Add(projectA);
        _lexBoxDbContext.Add(projectB);
        await _lexBoxDbContext.SaveChangesAsync();
        _autoCompleteMigration = false;

        var srToken = await _repoMigrationService.BeginSendReceive(projectB.Code);
        srToken.ShouldNotBeNull();

        _repoMigrationService.QueueMigration(projectA.Code);
        await ExpectMigrationCalled();
    }
}
