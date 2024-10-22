using LcmCrdt.Tests.Mocks;
using Meziantou.Extensions.Logging.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MiniLcm;
using MiniLcm.Models;
using Xunit.Abstractions;

namespace LcmCrdt.Tests;

public class MiniLcmApiFixture : IAsyncLifetime
{
    private AsyncServiceScope _services;
    private LcmCrdtDbContext? _crdtDbContext;
    public CrdtMiniLcmApi Api => (CrdtMiniLcmApi)_services.ServiceProvider.GetRequiredService<IMiniLcmApi>();
    public DataModel DataModel => _services.ServiceProvider.GetRequiredService<DataModel>();

    public MiniLcmApiFixture()
    {
    }

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection()
            .AddLcmCrdtClient()
            .AddLogging(builder => builder.AddDebug()
                .AddProvider(new LateXUnitLoggerProvider(this))
                .AddFilter("LinqToDB", LogLevel.Trace)
                .SetMinimumLevel(LogLevel.Error))
            .RemoveAll(typeof(ProjectContext))
            .AddSingleton<ProjectContext>(new MockProjectContext(new CrdtProject("sena-3", "test.sqlite")))
            .BuildServiceProvider();
        _services = services.CreateAsyncScope();
        _crdtDbContext = _services.ServiceProvider.GetRequiredService<LcmCrdtDbContext>();
        await _crdtDbContext.Database.OpenConnectionAsync();
        //can't use ProjectsService.CreateProject because it opens and closes the db context, this would wipe out the in memory db.
        await ProjectsService.InitProjectDb(_crdtDbContext,
            new ProjectData("Sena 3", Guid.NewGuid(), null, Guid.NewGuid()));
        await _services.ServiceProvider.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache();

        await Api.CreateWritingSystem(WritingSystemType.Vernacular,
            new WritingSystem()
            {
                Id = Guid.NewGuid(),
                WsId = "en",
                Name = "English",
                Abbreviation = "en",
                Font = "Arial",
                Exemplars = ["a", "b"],
                Type = WritingSystemType.Vernacular
            });
        await Api.CreateWritingSystem(WritingSystemType.Analysis,
            new WritingSystem()
            {
                Id = Guid.NewGuid(),
                WsId = "en",
                Name = "English",
                Abbreviation = "en",
                Font = "Arial",
                Type = WritingSystemType.Analysis
            });
    }

    ITestOutputHelper? _outputHelper;
    public void LogTo(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    private class LateXUnitLoggerProvider(MiniLcmApiFixture fixture) : ILoggerProvider
    {
        private ILoggerProvider? _loggerProvider;
        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (_loggerProvider is null)
            {
                if (fixture._outputHelper is null)
                {
                    return NullLogger.Instance;
                }
                _loggerProvider = new XUnitLoggerProvider(fixture._outputHelper, new XUnitLoggerOptions()
                {
                    IncludeCategory = true
                });
            }
            return _loggerProvider.CreateLogger(categoryName);
        }
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
    }
}
