using System.Diagnostics;
using LcmCrdt.MediaServer;
using Meziantou.Extensions.Logging.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace LcmCrdt.Tests;

public class MiniLcmApiFixture : IAsyncLifetime, IAsyncDisposable
{
    private readonly bool _seedWs = true;
    private AsyncServiceScope _services;
    private LcmCrdtDbContext? _crdtDbContext;
    public CrdtMiniLcmApi Api => (CrdtMiniLcmApi)_services.ServiceProvider.GetRequiredService<IMiniLcmApi>();
    public DataModel DataModel => _services.ServiceProvider.GetRequiredService<DataModel>();
    public LcmCrdtDbContext DbContext => _crdtDbContext ?? throw new InvalidOperationException("MiniLcmApiFixture not initialized");
    public CrdtConfig CrdtConfig => _services.ServiceProvider.GetRequiredService<IOptions<CrdtConfig>>().Value;

    public T GetService<T>() where T : notnull
    {
        return _services.ServiceProvider.GetRequiredService<T>();
    }

    //must have an empty constructor for xunit
    public MiniLcmApiFixture()
    {
    }

    public static MiniLcmApiFixture Create(bool seedWs = true)
    {
        return new MiniLcmApiFixture(seedWs);
    }

    private MiniLcmApiFixture(bool seedWs = true)
    {
        _seedWs = seedWs;
    }

    public async Task InitializeAsync()
    {
        await InitializeAsync("sena-3");
    }

    public async Task InitializeAsync(string projectName)
    {
        var db = $"file:{Guid.NewGuid():N}?mode=memory&cache=shared" ;
        if (Debugger.IsAttached)
        {
            db = "test.db";
            if (File.Exists(db))
            {
                File.Delete(db);
            }
        }

        var crdtProject = new CrdtProject(projectName, db);
        var services = new ServiceCollection()
            .AddTestLcmCrdtClient(crdtProject)
            .AddLogging(builder => builder.AddDebug()
                .AddProvider(new LateXUnitLoggerProvider(this))
                .AddFilter("LinqToDB", LogLevel.Trace)
                .SetMinimumLevel(LogLevel.Error))
            .BuildServiceProvider();
        _services = services.CreateAsyncScope();
        _crdtDbContext = await _services.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        await _crdtDbContext.Database.OpenConnectionAsync();
        //can't use ProjectsService.CreateProject because it opens and closes the db context, this would wipe out the in memory db.
        await CrdtProjectsService.InitProjectDb(_crdtDbContext,
            new ProjectData("Sena 3", projectName, Guid.NewGuid(), null, Guid.NewGuid()));
        await _services.ServiceProvider.GetRequiredService<CurrentProjectService>().RefreshProjectData();
        if (_seedWs)
        {
            await Api.CreateWritingSystem(new WritingSystem()
            {
                Id = Guid.NewGuid(),
                WsId = "en",
                Name = "English",
                Abbreviation = "en",
                Font = "Arial",
                Exemplars = ["a", "b"],
                Type = WritingSystemType.Vernacular
            });
            await Api.CreateWritingSystem(new WritingSystem()
            {
                Id = Guid.NewGuid(),
                WsId = "en",
                Name = "English",
                Abbreviation = "en",
                Font = "Arial",
                Type = WritingSystemType.Analysis
            });
        }
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
        var projectResourceCachePath = _services.ServiceProvider.GetRequiredService<LcmMediaService>().ProjectResourceCachePath;
        if (Directory.Exists(projectResourceCachePath)) Directory.Delete(projectResourceCachePath, true);
        await (_crdtDbContext?.DisposeAsync() ?? ValueTask.CompletedTask);
        await _services.DisposeAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
    }
}
