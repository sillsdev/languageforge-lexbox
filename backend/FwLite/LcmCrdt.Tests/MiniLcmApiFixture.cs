using LcmCrdt.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MiniLcm;

namespace LcmCrdt.Tests;

public class MiniLcmApiFixture : IAsyncLifetime
{
    private readonly AsyncServiceScope _services;
    private readonly LcmCrdtDbContext _crdtDbContext;
    public CrdtMiniLcmApi Api => (CrdtMiniLcmApi)_services.ServiceProvider.GetRequiredService<IMiniLcmApi>();
    public DataModel DataModel => _services.ServiceProvider.GetRequiredService<DataModel>();

    public MiniLcmApiFixture()
    {
        var services = new ServiceCollection()
            .AddLcmCrdtClient()
            .AddLogging(builder => builder.AddDebug())
            .RemoveAll(typeof(ProjectContext))
            .AddSingleton<ProjectContext>(new MockProjectContext(new CrdtProject("sena-3", ":memory:")))
            .BuildServiceProvider();
        _services = services.CreateAsyncScope();
        _crdtDbContext = _services.ServiceProvider.GetRequiredService<LcmCrdtDbContext>();
    }

    public async Task InitializeAsync()
    {
        await _crdtDbContext.Database.OpenConnectionAsync();
        //can't use ProjectsService.CreateProject because it opens and closes the db context, this would wipe out the in memory db.
        await ProjectsService.InitProjectDb(_crdtDbContext,
            new ProjectData("Sena 3", Guid.NewGuid(), null, Guid.NewGuid()));
        await _services.ServiceProvider.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
    }
}
