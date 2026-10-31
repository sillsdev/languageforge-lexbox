using SIL.Harmony.Config;
using System.Diagnostics;
using LcmCrdt.Changes;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using LinqToDB.Mapping;
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
    private readonly Action<IServiceCollection>? _configureServices;
    // fixtures run in parallel and share project codes, so each gets its own folder (media cache) to delete on dispose
    private readonly string _projectPath = Path.GetFullPath($"fixture-{Guid.NewGuid():N}");
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

    public static MiniLcmApiFixture Create(bool seedWs = true, Guid? projectId = null, Action<IServiceCollection>? configureServices = null)
    {
        return new MiniLcmApiFixture(seedWs, projectId, configureServices);
    }

    private MiniLcmApiFixture(bool seedWs = true, Guid? projectId = null, Action<IServiceCollection>? configureServices = null)
    {
        _seedWs = seedWs;
        _projectId = projectId;
        _configureServices = configureServices;
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

        var serviceCollection = new ServiceCollection()
            .AddTestLcmCrdtClient()
            .Configure<LcmCrdtConfig>(config => config.ProjectPath = _projectPath)
            .AddLogging(builder => builder.AddDebug()
                .AddProvider(new LateXUnitLoggerProvider(this))
                .AddFilter("LinqToDB", LogLevel.Trace)
                .SetMinimumLevel(LogLevel.Error));
        if (_configureServices is not null)
        {
            _configureServices(serviceCollection);
            // Hook config like HarmonyConfig gets baked into the EF model and into linq2db's mapping schema, which
            // linq2db.EntityFrameworkCore caches process-wide by DataOptions equality (first fixture wins; mapping
            // schemas compare by content, so every fixture's looks the same). A uniquely named schema per fixture
            // keeps this fixture's config out of that cache. ConfigureDbContext layers onto the kernel's
            // registration; a second AddDbContextFactory is ignored.
            serviceCollection.ConfigureDbContext<LcmCrdtDbContext>(builder => builder
                .EnableServiceProviderCaching(false)
                .UseLinqToDB(linq2db =>
                {
                    var shared = linq2db.DbContextOptions.GetLinqToDBOptions()?.ConnectionOptions.MappingSchema ?? new MappingSchema();
                    linq2db.AddMappingSchema(new MappingSchema($"fixture-{Guid.NewGuid():N}", shared));
                    linq2db.AddCustomOptions(options => options.UseDisableQueryCache(true));
                }));
        }
        var services = serviceCollection.BuildServiceProvider();
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
        await (_crdtDbContext?.DisposeAsync() ?? ValueTask.CompletedTask);
        await _services.DisposeAsync();
        await (_keepAliveConnection?.DisposeAsync() ?? ValueTask.CompletedTask);
        if (Directory.Exists(_projectPath)) Directory.Delete(_projectPath, true);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
    }
}
