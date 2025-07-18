using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LcmCrdt.Tests.Project;

public class CurrentProjectServiceTests : IAsyncLifetime
{
    private readonly Guid _testInstance = Guid.NewGuid();
    private readonly string _testRoot = Path.GetFullPath("current-project-service-tests");
    private string TestProjectPath => Path.Join(_testRoot, _testInstance.ToString("N"));
    private readonly CurrentProjectService _service;
    private readonly ServiceProvider _services;
    private readonly AsyncServiceScope _serviceScope;

    public CurrentProjectServiceTests()
    {
        (_services, _serviceScope, _service) = NewInstance();
    }

    private (ServiceProvider, AsyncServiceScope, CurrentProjectService) NewInstance()
    {
        var services = new ServiceCollection()
            .AddTestLcmCrdtClient()
            .Configure<LcmCrdtConfig>(config =>
            {
                config.ProjectPath = TestProjectPath;
                config.ProjectCacheFileName = "test.cache";
            }).PostConfigure<LcmCrdtConfig>(c => c.EnableProjectDataFileCache = true)
            .BuildServiceProvider();
        var serviceScope = services.CreateAsyncScope();
        return (services, serviceScope, serviceScope.ServiceProvider.GetRequiredService<CurrentProjectService>());
    }

    private async Task<CrdtProject> CreateProject(string? name = null)
    {
        name ??= $"test-{_testInstance:N}";
        return await _serviceScope.ServiceProvider.GetRequiredService<CrdtProjectsService>().CreateExampleProject(name);
    }

    public Task InitializeAsync()
    {
        if (Directory.Exists(_testRoot)) Directory.Delete(_testRoot, true);
        if (!Directory.Exists(TestProjectPath)) Directory.CreateDirectory(TestProjectPath);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _serviceScope.DisposeAsync();
        await _services.DisposeAsync();
    }

    [Fact]
    public void ProjectThrowsWhenNotSetup()
    {
        var act = () => _service.Project;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ProjectWorksWhenSetup()
    {
        _service.SetupProjectContextForNewDb(new CrdtProject("sena-3", "test.sqlite"));
        _service.Project.Should().NotBeNull();
    }

    [Fact]
    public async Task ProjectDataWorksWhenSetup()
    {
        var project = await CreateProject("test-data-setup-works");
        await _service.SetupProjectContext(project);
        _service.ProjectData.Should().NotBeNull();
        _service.ProjectData.Name.Should().Be("test-data-setup-works");
    }

    [Fact]
    public async Task CanGetProjectDataWithoutSetupInNewInstance()
    {
        var project = await CreateProject("test-data-setup-works-in-new-instance");
        await _service.SetupProjectContext(project);
        var (_, _, service) = NewInstance();
        service.SetupProjectContextForNewDb(project);
        service.ProjectData.Should().NotBeNull();
        service.ProjectData.Name.Should().Be("test-data-setup-works-in-new-instance");
    }
}
