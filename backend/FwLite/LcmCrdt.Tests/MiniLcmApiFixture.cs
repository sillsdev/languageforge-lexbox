using SIL.Harmony.Config;
using System.Diagnostics;
using LcmCrdt.Changes;
using LcmCrdt.MediaServer;
using Meziantou.Extensions.Logging.Xunit;
using Microsoft.Data.Sqlite;
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
    private readonly Guid? _projectId;
    private AsyncServiceScope _services;
    private LcmCrdtDbContext? _crdtDbContext;
    private SqliteConnection? _keepAliveConnection;
    public CrdtMiniLcmApi Api => (CrdtMiniLcmApi)_services.ServiceProvider.GetRequiredService<IMiniLcmApi>();
    public DataModel DataModel => _services.ServiceProvider.GetRequiredService<DataModel>();
    public LcmCrdtDbContext DbContext => _crdtDbContext ?? throw new InvalidOperationException("MiniLcmApiFixture not initialized");
    public HarmonyConfig HarmonyConfig => _services.ServiceProvider.GetRequiredService<IOptions<HarmonyConfig>>().Value;

    public T GetService<T>() where T : notnull
    {
        return _services.ServiceProvider.GetRequiredService<T>();
    }

    //must have an empty constructor for xunit
    public MiniLcmApiFixture()
    {
    }

    public static MiniLcmApiFixture Create(bool seedWs = true, Guid? projectId = null)
    {
        return new MiniLcmApiFixture(seedWs, projectId);
    }

    private MiniLcmApiFixture(bool seedWs = true, Guid? projectId = null)
    {
        _seedWs = seedWs;
        _projectId = projectId;
    }

    public async Task InitializeAsync()
    {
        await InitializeAsync("sena-3", _projectId);
    }

    public async Task InitializeAsync(string projectName, Guid? projectId = null)
    {
        var db = $"file:{Guid.NewGuid():N}?mode=memory&cache=shared";
        var inMemory = true;
        if (Debugger.IsAttached)
        {
            db = "test.db";
            inMemory = false;
            if (File.Exists(db))
            {
                File.Delete(db);
            }
        }

        var services = new ServiceCollection()
            .AddTestLcmCrdtClient()
            .AddLogging(builder => builder.AddDebug()
                .AddProvider(new LateXUnitLoggerProvider(this))
                .AddFilter("LinqToDB", LogLevel.Trace)
                .SetMinimumLevel(LogLevel.Error))
            .BuildServiceProvider();
        _services = services.CreateAsyncScope();
        if (inMemory)
        {
            //an in memory db only lives as long as a connection to it is open, and CreateProject opens and
            //closes its own, so hold one open for the lifetime of the fixture to keep the db alive.
            _keepAliveConnection = new SqliteConnection($"Data Source={db}");
            await _keepAliveConnection.OpenAsync();
        }

        var crdtProject = await _services.ServiceProvider.GetRequiredService<CrdtProjectsService>()
            .CreateProject(new("Sena 3", projectName, projectId, DbPath: db));
        //same as opening the project in the app: migrate, regenerate search table if missing, load project data
        await _services.ServiceProvider.GetRequiredService<CurrentProjectService>().SetupProjectContext(crdtProject);
        _crdtDbContext = await _services.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        // CreateProject no longer seeds morph types on migrate (#2350). This fixture bypasses
        // CreateProjectFromTemplate, so seed them for tests that depend on them (sorting,
        // homograph numbers, morph-token search, MorphTypeTestsBase).
        await DataModel.AddChanges(crdtProject.Data!.ClientId,
            [.. CanonicalMorphTypes.All.Values.Select(mt => new CreateMorphTypeChange(mt))]);
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
        await (_keepAliveConnection?.DisposeAsync() ?? ValueTask.CompletedTask);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
    }
}
