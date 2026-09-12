using SIL.Harmony.Config;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using LinqToDB.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MiniLcm.Culture;
using SIL.Harmony;
using SIL.Harmony.Db;

namespace LcmCrdt.Data;

public class SetupCollationInterceptor(IMemoryCache cache, IMiniLcmCultureProvider cultureProvider, IOptions<HarmonyConfig> harmonyConfig)
    : IDbConnectionInterceptor, ISaveChangesInterceptor, IConnectionInterceptor, IProjectedEntityInterceptor
{
    private static string? WsTableName = null;

    // Cached writing-system lists are tied to this token so they can all be dropped at once whenever a
    // writing system is added or changed — otherwise a connection that cached the list before the change
    // (its cache key includes the connection string, so it isn't cleared by a per-connection invalidation
    // on the writing connection) would keep registering a stale set of collations.
    private CancellationTokenSource _writingSystemsCacheReset = new();
    private WritingSystem[] GetWritingSystems(DbConnection connection, LcmCrdtDbContext? dbContext = null)
    {
        var cacheKey = CacheKey(connection);
        if (cache.TryGetValue<WritingSystem[]>(cacheKey, out var cached) && cached is { Length: > 0 })
            return cached;

        try
        {
            var localContext = dbContext;
            if (localContext is null)
            {
                var optionsBuilder = new DbContextOptionsBuilder<LcmCrdtDbContext>();
                optionsBuilder.UseSqlite(connection);
                localContext = new LcmCrdtDbContext(optionsBuilder.Options, harmonyConfig);
            }

            try
            {
                WsTableName ??= localContext.Model.FindRuntimeEntityType(typeof(WritingSystem))?.GetTableName() ?? "WritingSystem";
                if (!HasTable(localContext, WsTableName))
                {
                    return [];
                }

                var writingSystems = localContext.WritingSystems.ToArray();
                // Only cache a non-empty result. Caching an empty list would poison the cache: a
                // connection that opens before writing systems are populated (e.g. during database
                // initialization) would otherwise pin an empty result for the sliding-expiration window,
                // so writing-system collations would never get registered once the rows exist — leading
                // to "no such collation sequence" errors on later queries. An empty result is cheap to
                // recompute and means there are no writing-system collations to register anyway.
                if (writingSystems.Length > 0)
                {
                    var options = new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(30) };
                    options.AddExpirationToken(new CancellationChangeToken(_writingSystemsCacheReset.Token));
                    cache.Set(cacheKey, writingSystems, options);
                }
                return writingSystems;
            }
            finally
            {
                if (dbContext is null)
                {
                    localContext.Dispose();
                }
            }
        }
        catch (SqliteException)
        {
            return [];
        }
    }

    /// <summary>
    /// Ensures the general-use and writing-system-specific collations are registered on the given
    /// context's connection. Call this immediately before running a query that relies on a
    /// writing-system collation (e.g. sorting/filtering headwords).
    /// </summary>
    /// <remarks>
    /// The connection-opened interceptors register collations when a connection opens, but that is not
    /// enough on its own: a connection can be opened before the writing systems (or even the schema)
    /// exist, and it is then reused for later queries without the interceptor firing again. Registering
    /// collations here — on the connection that is about to run the query — makes collation availability
    /// independent of when the connection happened to open.
    /// </remarks>
    public async ValueTask EnsureCollationsSetup(LcmCrdtDbContext dbContext)
    {
        var connection = (SqliteConnection)dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await dbContext.Database.OpenConnectionAsync();
        SetupCommonCollations(connection, GetWritingSystems(connection, dbContext));
    }

    private bool HasTable(DbContext context, string tableName)
    {
        if (!context.Database.IsSqlite()) throw new InvalidOperationException($"HasTable only works with sqlite, update it to support {context.Database.ProviderName}");
        var result = context.Database.SqlQuery<int>($"SELECT 1 FROM sqlite_master WHERE type='table' AND name={tableName}").ToArray();
        return result.Length > 0 && result[0] > 0;
    }

    private static string CacheKey(DbConnection connection)
    {
        return $"writingSystems|{connection.ConnectionString}";
    }

    /// <summary>
    /// Drops every cached writing-system list (across all connections) so the next query re-reads the
    /// current set and registers any newly added writing-system collation.
    /// </summary>
    private void InvalidateAllWritingSystemsCaches()
    {
        var previous = Interlocked.Exchange(ref _writingSystemsCacheReset, new CancellationTokenSource());
        // Cancelling evicts every cache entry linked to this token. Deliberately not disposed: a
        // concurrent GetWritingSystems may still read its Token, and a cancelled source with no
        // registrations left is cheap and collected once unreferenced.
        previous.Cancel();
    }

    // Harmony projects writing-system changes with raw SQL, bypassing EF change tracking, so the
    // ISaveChangesInterceptor path below never sees them for API-driven writes. React to the projected
    // change here instead: invalidate the cached writing-system lists so later queries pick up the new
    // writing system and register its collation.
    public ValueTask OnProjectedEntitiesChanged(ProjectedEntityBatch batch)
    {
        if (batch.Changes.Any(c => c.ClrType == typeof(WritingSystem)))
            InvalidateAllWritingSystemsCaches();
        return ValueTask.CompletedTask;
    }

    private void SetupCommonCollations(SqliteConnection sqliteConnection, WritingSystem[]? writingSystems = null)
    {
        // Setup general use collation
        sqliteConnection.CreateCollation(SqlSortingExtensions.CollateUnicodeNoCase,
            CultureInfo.CurrentCulture.CompareInfo,
            (compareInfo, x, y) =>
            {
                var caseInsensitiveResult = compareInfo.Compare(x, y, CompareOptions.IgnoreCase);
                if (caseInsensitiveResult != 0)
                    return caseInsensitiveResult;
                // When case-insensitively equal, sort lowercase before uppercase
                return compareInfo.Compare(x, y, CompareOptions.None);
            });

        // Setup writing system specific collations if available
        if (writingSystems is not null)
        {
            SetupCollations(sqliteConnection, writingSystems);
        }
    }

    public void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        var context = (LcmCrdtDbContext?)eventData.Context;
        if (context is null) throw new InvalidOperationException("context is null");
        var sqliteConnection = (SqliteConnection)connection;
        SetupCommonCollations(sqliteConnection, GetWritingSystems(connection, context));
    }

    public Task ConnectionOpenedAsync(DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ConnectionOpened(connection, eventData);
        return Task.CompletedTask;
    }

    // LinqToDB interface
    public void ConnectionOpening(LinqToDB.Interceptors.ConnectionEventData eventData, DbConnection connection)
    {
        // Setup happens after connection opens
    }

    public Task ConnectionOpeningAsync(LinqToDB.Interceptors.ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void ConnectionOpened(LinqToDB.Interceptors.ConnectionEventData eventData, DbConnection connection)
    {
        if (connection is not SqliteConnection sqliteConnection) return;

        // Collations persist on the connection, so if EF already opened this connection, this is
        // redundant but harmless. SQLite allows re-registering collations.
        SetupCommonCollations(sqliteConnection, GetWritingSystems(connection));
    }

    public Task ConnectionOpenedAsync(LinqToDB.Interceptors.ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
    {
        ConnectionOpened(eventData, connection);
        return Task.CompletedTask;
    }

    public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateCollationsOnSave(eventData.Context);
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateCollationsOnSave(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void UpdateCollationsOnSave(DbContext? dbContext)
    {
        if (dbContext is null) return;
        var connection = (SqliteConnection)dbContext.Database.GetDbConnection();
        bool updateWs = false;
        foreach (var entityEntry in dbContext.ChangeTracker.Entries<WritingSystem>())
        {
            if (entityEntry.State is EntityState.Added or EntityState.Modified)
            {
                // The connection might not yet be open if ef is just getting ready to save stuff
                if (connection.State != ConnectionState.Open) connection.Open();

                var writingSystem = entityEntry.Entity;
                SetupCollation(connection, writingSystem);
                updateWs = true;
            }
        }

        if (updateWs)
        {
            // Drop every connection's cached list, not just this one's, so reads on other connections
            // re-read the new writing system and register its collation.
            InvalidateAllWritingSystemsCaches();
        }
    }

    private void SetupCollations(SqliteConnection connection, WritingSystem[] writingSystems)
    {
        foreach (var writingSystem in writingSystems)
        {
            SetupCollation(connection, writingSystem);
        }
    }

    private void SetupCollation(SqliteConnection connection, WritingSystem writingSystem)
    {
        var compareInfo = cultureProvider.GetCompareInfo(writingSystem);

        //todo use custom comparison based on the writing system
        CreateSpanCollation(connection, SqlSortingExtensions.CollationName(writingSystem.WsId),
            compareInfo,
            static (compareInfo, x, y) =>
            {
                var caseInsensitiveResult = compareInfo.Compare(x, y, CompareOptions.IgnoreCase);
                if (caseInsensitiveResult != 0)
                    return caseInsensitiveResult;
                // When case-insensitively equal, sort lowercase before uppercase
                return compareInfo.Compare(x, y, CompareOptions.None);
            });
    }

    //this is a premature optimization, but it avoids creating strings for each comparison and instead uses spans which avoids allocations
    //if the new comparison function does not support spans then we can use SqliteConnection.CreateCollation instead which works with strings
    private void CreateSpanCollation<T>(SqliteConnection connection,
        string name, T state,
        Func<T, ReadOnlySpan<char>, ReadOnlySpan<char>, int> compare)
    {
        if (connection.State != ConnectionState.Open)
            throw new InvalidOperationException("Unable to create custom collation Connection must be open.");
        var rc = SQLitePCL.raw.sqlite3__create_collation_utf8(connection.Handle,
            name,
            Tuple.Create(state, compare),
            static (s, x, y) =>
            {
                var (state, compare) = (Tuple<T, Func<T, ReadOnlySpan<char>, ReadOnlySpan<char>, int>>) s;
                Span<char> xSpan = stackalloc char[Encoding.UTF8.GetCharCount(x)];
                Span<char> ySpan = stackalloc char[Encoding.UTF8.GetCharCount(y)];
                Encoding.UTF8.GetChars(x, xSpan);
                Encoding.UTF8.GetChars(y, ySpan);

                return compare(state, xSpan, ySpan);
            });
        SqliteException.ThrowExceptionForRC(rc, connection.Handle);

    }
}
