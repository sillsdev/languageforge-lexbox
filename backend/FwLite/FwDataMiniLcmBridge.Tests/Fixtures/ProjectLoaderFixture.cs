using FwDataMiniLcmBridge.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FwDataMiniLcmBridge.Tests.Fixtures;

public class ProjectLoaderFixture : IDisposable
{
    public const string Name = "ProjectLoaderCollection";
    private readonly FwDataFactory _fwDataFactory;
    private readonly ServiceProvider _serviceProvider;
    private readonly IOptions<FwDataBridgeConfig> _config;
    public MockFwProjectLoader MockFwProjectLoader { get; }

    public ProjectLoaderFixture()
    {
        //todo make mock of IProjectLoader so we can load from test projects
        var provider = new ServiceCollection().AddTestFwDataBridge().BuildServiceProvider();
        _serviceProvider = provider;
        _fwDataFactory = provider.GetRequiredService<FwDataFactory>();
        MockFwProjectLoader = provider.GetRequiredService<MockFwProjectLoader>();
        _config = provider.GetRequiredService<IOptions<FwDataBridgeConfig>>();
    }

    public FwDataMiniLcmApi CreateApi(string projectName)
    {
        return _fwDataFactory.GetFwDataMiniLcmApi(projectName, false);
    }

    public FwDataMiniLcmApi NewProjectApi(string projectName, string analysisWs, string vernacularWs)
    {
        projectName = $"{projectName}_{Guid.NewGuid()}";
        MockFwProjectLoader.NewProject(new FwDataProject(projectName, _config.Value.ProjectsFolder), analysisWs, vernacularWs);
        return CreateApi(projectName);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}

[CollectionDefinition(ProjectLoaderFixture.Name)]
public class ProjectLoaderCollection : ICollectionFixture<ProjectLoaderFixture>
{
}
