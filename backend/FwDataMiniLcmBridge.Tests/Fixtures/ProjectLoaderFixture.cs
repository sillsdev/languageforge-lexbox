using FwDataMiniLcmBridge.Api;
using Microsoft.Extensions.DependencyInjection;

namespace FwDataMiniLcmBridge.Tests.Fixtures;

public class ProjectLoaderFixture : IDisposable
{
    private readonly FwDataFactory _fwDataFactory;
    private ServiceProvider _serviceProvider;

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
        _fwDataFactory.Dispose();
        _serviceProvider.Dispose();
    }
}
