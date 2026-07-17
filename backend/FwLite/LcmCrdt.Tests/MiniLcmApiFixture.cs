using System.Collections.Concurrent;
using System.Diagnostics;
using LcmCrdt.MediaServer;
using LcmCrdt.Objects;
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
    private SqliteConnection? _dbKeepAlive;
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
        var useTemplate = true;
        if (Debugger.IsAttached)
        {
            db = "test.db";
            if (File.Exists(db))
            {
                File.Delete(db);
            }
            // Full init keeps the on-disk test.db self-contained for debugging.
            useTemplate = false;
        }

        var crdtProject = new CrdtProject(projectName, db);

        if (useTemplate)
        {
            // Fast path: byte-copy a pre-seeded template db (schema + 23 migrations + morph-types + FTS
            // + optional writing systems) into this fresh in-memory db via the SQLite backup API, instead
            // of replaying all of that per test. Cuts per-test setup from ~400ms to ~50ms. The template is
            // built once per (projectName, seedWs) and cached for the whole test run.
            //
            // The copy MUST happen before EF opens its connection: the collation interceptor registers
            // writing-system collations (e.g. NOCASE_WS_en) from the writing systems present at
            // ConnectionOpened time, so the seeded rows have to already be in the db. A dedicated keepalive
            // connection keeps the shared-cache in-memory db alive across the copy and for the fixture's life.
            _dbKeepAlive = new SqliteConnection($"Data Source={db}");
            await _dbKeepAlive.OpenAsync();
            var template = await GetTemplate(projectName, _seedWs);
            await template.CopyToAsync(_dbKeepAlive);
        }

        var services = new ServiceCollection()
            .AddTestLcmCrdtClient(crdtProject)
            .AddLogging(builder => builder.AddDebug()
                .AddProvider(new LateXUnitLoggerProvider(this))
                .AddFilter("LinqToDB", LogLevel.Trace)
                .SetMinimumLevel(LogLevel.Error))
            .BuildServiceProvider();
        _services = services.CreateAsyncScope();
        var currentProjectService = _services.ServiceProvider.GetRequiredService<CurrentProjectService>();
        currentProjectService.SetupProjectContextForNewDb(crdtProject);
        _crdtDbContext = await _services.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        await _crdtDbContext.Database.OpenConnectionAsync();

        if (useTemplate)
        {
            // The template bakes a placeholder project id; give each fixture its own identity, honoring an
            // explicit projectId (e.g. DownloadProjectTests) exactly as the full-init path did.
            await _crdtDbContext.Database.ExecuteSqlAsync($"UPDATE \"ProjectData\" SET \"Id\" = {projectId ?? Guid.NewGuid()}");
            // Loads ProjectData into the cache; MigrateAsync/morph-type/FTS steps are no-ops because the
            // migrations history and seeded rows were copied.
            await currentProjectService.SetupProjectContext(crdtProject);
            return;
        }

        //can't use ProjectsService.CreateProject because it opens and closes the db context, this would wipe out the in memory db.
        var projectData = new ProjectData("Sena 3", projectName, projectId ?? Guid.NewGuid(), null, Guid.NewGuid());
        await CrdtProjectsService.InitProjectDb(_crdtDbContext, projectData);
        // Also trigger "data migrations" that CreateProject runs
        await currentProjectService.SetupProjectContext(crdtProject);
        if (_seedWs)
        {
            await SeedWritingSystems(Api);
        }
    }

    private static async Task SeedWritingSystems(IMiniLcmApi api)
    {
        await api.CreateWritingSystem(new WritingSystem()
        {
            Id = Guid.NewGuid(),
            WsId = "en",
            Name = "English",
            Abbreviation = "en",
            Font = "Arial",
            Exemplars = ["a", "b"],
            Type = WritingSystemType.Vernacular
        });
        await api.CreateWritingSystem(new WritingSystem()
        {
            Id = Guid.NewGuid(),
            WsId = "en",
            Name = "English",
            Abbreviation = "en",
            Font = "Arial",
            Type = WritingSystemType.Analysis
        });
    }

    #region Seeded template cache

    // A pre-seeded in-memory db kept alive for the whole test run. Tests byte-copy it via the SQLite backup
    // API rather than migrating + seeding from scratch. Keyed by (projectName, seedWs) so each shape is
    // built at most once.
    private static readonly ConcurrentDictionary<(string projectName, bool seedWs), Lazy<Task<Template>>> Templates = new();

    private static Task<Template> GetTemplate(string projectName, bool seedWs)
    {
        // Lazy<Task<T>> ensures each template shape is built exactly once even under parallel access.
        // See https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
#pragma warning disable VSTHRD011
        return Templates.GetOrAdd((projectName, seedWs),
            key => new Lazy<Task<Template>>(() => BuildTemplate(key.projectName, key.seedWs))).Value;
#pragma warning restore VSTHRD011
    }

    private static async Task<Template> BuildTemplate(string projectName, bool seedWs)
    {
        var db = $"file:template_{Guid.NewGuid():N}?mode=memory&cache=shared";
        // Held open for the process lifetime: a shared-cache in-memory db only survives while at least one
        // connection is open. This is the backup source for every copy.
        var keepAlive = new SqliteConnection($"Data Source={db}");
        await keepAlive.OpenAsync();

        var crdtProject = new CrdtProject(projectName, db);
        await using var services = new ServiceCollection()
            .AddTestLcmCrdtClient(crdtProject)
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Error))
            .BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var currentProjectService = scope.ServiceProvider.GetRequiredService<CurrentProjectService>();
        currentProjectService.SetupProjectContextForNewDb(crdtProject);
        await using var ctx = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        await ctx.Database.OpenConnectionAsync();
        await CrdtProjectsService.InitProjectDb(ctx, new ProjectData("Sena 3", projectName, Guid.NewGuid(), null, Guid.NewGuid()));
        await currentProjectService.SetupProjectContext(crdtProject);
        if (seedWs)
        {
            await SeedWritingSystems((CrdtMiniLcmApi)scope.ServiceProvider.GetRequiredService<IMiniLcmApi>());
        }
        return new Template(keepAlive);
    }

    private sealed class Template(SqliteConnection keepAlive)
    {
        // BackupDatabase is not safe to run concurrently on one connection; serialize the (sub-millisecond)
        // copies across parallel tests.
        private readonly SemaphoreSlim _copyLock = new(1, 1);

        public async Task CopyToAsync(SqliteConnection destination)
        {
            await _copyLock.WaitAsync();
            try
            {
                keepAlive.BackupDatabase(destination);
            }
            finally
            {
                _copyLock.Release();
            }
        }
    }

    #endregion

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
        await (_dbKeepAlive?.DisposeAsync() ?? ValueTask.CompletedTask);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
    }
}
