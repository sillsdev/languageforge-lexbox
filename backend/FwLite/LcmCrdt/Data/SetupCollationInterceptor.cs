using SIL.Harmony.Config;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
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
using SQLitePCL;

namespace LcmCrdt.Data;

public class SetupCollationInterceptor(
    IMemoryCache cache,
    IMiniLcmCultureProvider cultureProvider,
    IWritingSystemCollatorProvider collatorProvider,
    IOptions<HarmonyConfig> harmonyConfig)
    : IDbConnectionInterceptor, ISaveChangesInterceptor, IConnectionInterceptor, IProjectedEntityInterceptor
{
    private static string? WsTableName = null;

    // What has been registered on each native connection handle. Keyed by the sqlite3 handle rather than
    // the SqliteConnection because pooled handles outlive SqliteConnection objects and keep collations
    // registered directly on the handle, so a reused handle needs no re-registration.
    private readonly ConditionalWeakTable<sqlite3, RegisteredCollations> _registeredCollations = new();

    // WritingSystemsVersion is null when no writing-system collations were registered (none existed yet),
    // and is cancelled when that project's writing systems change, marking the handle's set as stale.
    private sealed record RegisteredCollations(CancellationToken? WritingSystemsVersion);

    // One reset source per connection string (i.e. per project database). Cancelling it evicts that
    // project's cached writing-system list and marks its handles' writing-system collations as stale,
    // without touching other projects.
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _writingSystemsResets = new();

    private CancellationTokenSource WritingSystemsReset(DbConnection connection) =>
        _writingSystemsResets.GetOrAdd(connection.ConnectionString, _ => new CancellationTokenSource());

    private void InvalidateWritingSystems(DbConnection connection)
    {
        // Cancelling evicts every cache entry and handle linked to this token. Deliberately not disposed:
        // a concurrent reader may still read its Token, and a cancelled source is collected once unreferenced.
        if (_writingSystemsResets.TryRemove(connection.ConnectionString, out var previous))
            previous.Cancel();
    }

    private WritingSystem[] GetWritingSystems(DbConnection connection, CancellationToken cacheVersion, LcmCrdtDbContext? dbContext = null)
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
                    // Schema not migrated yet — don't cache so a later open can register collations.
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
                    options.AddExpirationToken(new CancellationChangeToken(cacheVersion));
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
    /// independent of when the connection happened to open. Cheap once the handle is up to date.
    /// </remarks>
    public async ValueTask EnsureCollationsSetup(LcmCrdtDbContext dbContext)
    {
        var connection = (SqliteConnection)dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await dbContext.Database.OpenConnectionAsync();
        EnsureCollations(connection, dbContext);
    }

    private void EnsureCollations(SqliteConnection connection, LcmCrdtDbContext? dbContext = null)
    {
        var handle = connection.Handle ?? throw new InvalidOperationException("Unable to setup collations, connection must be open.");
        _registeredCollations.TryGetValue(handle, out var registered);
        if (registered?.WritingSystemsVersion is { IsCancellationRequested: false }) return;

        // Capture the version before reading the list. If a concurrent writing-system change cancels it
        // after we've read the (now stale) list, both the cache entry and this handle are marked stale
        // by that same invalidation rather than surviving it.
        var version = WritingSystemsReset(connection).Token;
        if (registered is null) SetupCommonCollation(connection);
        var writingSystems = GetWritingSystems(connection, version, dbContext);
        SetupCollations(connection, writingSystems);
        _registeredCollations.AddOrUpdate(handle, new RegisteredCollations(writingSystems.Length > 0 ? version : null));
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

    // Harmony projects writing-system changes with raw SQL, bypassing EF change tracking, so the
    // ISaveChangesInterceptor path below never sees them for API-driven writes. React to the projected
    // change here instead, the same way the save path does.
    public ValueTask OnProjectedEntitiesChanged(ProjectedEntityBatch batch)
    {
        if (batch.DbContext is not LcmCrdtDbContext dbContext) return ValueTask.CompletedTask;
        var changes = batch.Changes.Where(c => c.ClrType == typeof(WritingSystem)).ToArray();
        if (changes.Length == 0) return ValueTask.CompletedTask;

        var upserted = changes
            .Where(c => c.Kind == ProjectedChangeKind.Upsert)
            .Select(c => c.Entity)
            .OfType<WritingSystem>();
        OnWritingSystemsChanged((SqliteConnection)dbContext.Database.GetDbConnection(), upserted);
        return ValueTask.CompletedTask;
    }

    // Registers the added/changed writing systems' collations on the writing connection, then marks this
    // project's cached list and handles as stale so other connections re-read and register them too.
    private void OnWritingSystemsChanged(SqliteConnection connection, IEnumerable<WritingSystem> upserted)
    {
        // The connection might not yet be open if ef is just getting ready to save stuff
        if (connection.State != ConnectionState.Open) connection.Open();
        SetupCollations(connection, upserted);
        InvalidateWritingSystems(connection);
    }

    private void SetupCommonCollation(SqliteConnection sqliteConnection)
    {
        // Registered on the handle rather than via SqliteConnection.CreateCollation, which Microsoft.Data.Sqlite
        // unregisters when the connection returns to the pool, so it lives exactly as long as the handle
        // tracked in _registeredCollations.
        CreateSpanCollation(sqliteConnection, SqlSortingExtensions.CollateUnicodeNoCase,
            CultureInfo.CurrentCulture.CompareInfo,
            CompareIgnoreCaseLowerFirst);
    }

    public void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        var context = (LcmCrdtDbContext?)eventData.Context;
        if (context is null) throw new InvalidOperationException("context is null");
        EnsureCollations((SqliteConnection)connection, context);
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

        // A no-op when EF (or an earlier use of this pooled handle) already registered the collations.
        EnsureCollations(sqliteConnection);
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
        var upserted = dbContext.ChangeTracker.Entries<WritingSystem>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Select(e => e.Entity)
            .ToArray();
        if (upserted.Length == 0) return;
        OnWritingSystemsChanged((SqliteConnection)dbContext.Database.GetDbConnection(), upserted);
    }

    private void SetupCollations(SqliteConnection connection, IEnumerable<WritingSystem> writingSystems)
    {
        foreach (var writingSystem in writingSystems)
        {
            SetupCollation(connection, writingSystem);
        }
    }

    private void SetupCollation(SqliteConnection connection, WritingSystem writingSystem)
    {
        if (HasImportedCollation(writingSystem))
        {
            // ICollator only compares strings, so this path allocates per comparison.
            CreateSpanCollation(connection, SqlSortingExtensions.CollationName(writingSystem.WsId),
                collatorProvider.GetCollator(writingSystem),
                static (collator, x, y) => collator.Compare(x.ToString(), y.ToString()));
            return;
        }

        var compareInfo = cultureProvider.GetCompareInfo(writingSystem);

        CreateSpanCollation(connection, SqlSortingExtensions.CollationName(writingSystem.WsId),
            compareInfo,
            CompareIgnoreCaseLowerFirst);
    }

    private static int CompareIgnoreCaseLowerFirst(CompareInfo compareInfo, ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        var caseInsensitiveResult = compareInfo.Compare(x, y, CompareOptions.IgnoreCase);
        if (caseInsensitiveResult != 0)
            return caseInsensitiveResult;
        // When case-insensitively equal, sort lowercase before uppercase
        return compareInfo.Compare(x, y, CompareOptions.None);
    }

    private static bool HasImportedCollation(WritingSystem writingSystem) =>
        !string.IsNullOrEmpty(writingSystem.SystemCollationLocale)
        || !string.IsNullOrEmpty(writingSystem.IcuCollationRules);

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
