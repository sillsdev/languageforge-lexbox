using FwDataMiniLcmBridge.Api;
using Microsoft.Extensions.DependencyInjection;

namespace FwDataMiniLcmBridge.Tests.Fixtures;

public class ProjectLoaderFixture : IDisposable
{
    public const string Name = "ProjectLoaderCollection";
    private readonly FwDataFactory _fwDataFactory;
    private readonly ServiceProvider _serviceProvider;

    public ProjectLoaderFixture()
    {
        //todo make mock of IProjectLoader so we can load from test projects
        var provider = new ServiceCollection().AddFwDataBridge().BuildServiceProvider();
        _serviceProvider = provider;
        _fwDataFactory = provider.GetRequiredService<FwDataFactory>();
    }

    public FwDataMiniLcmApi CreateApi(string projectName)
    {
        return _fwDataFactory.GetFwDataMiniLcmApi(projectName, false);
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
