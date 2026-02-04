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
using MiniLcm.Culture;
using SIL.Harmony;

namespace LcmCrdt.Data;

public class SetupCollationInterceptor(IMemoryCache cache, IMiniLcmCultureProvider cultureProvider, IOptions<CrdtConfig> crdtConfig) : IDbConnectionInterceptor, ISaveChangesInterceptor, IConnectionInterceptor
{
    private static string? WsTableName = null;
    private WritingSystem[] GetWritingSystems(DbConnection connection, LcmCrdtDbContext? dbContext = null)
    {
        return cache.GetOrCreate(CacheKey(connection),
            entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(30);
                try
                {
                    var localContext = dbContext;
                    if (localContext is null)
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<LcmCrdtDbContext>();
                        optionsBuilder.UseSqlite(connection);
                        localContext = new LcmCrdtDbContext(optionsBuilder.Options, crdtConfig);
                    }

                    try
                    {
                        WsTableName ??= localContext.Model.FindRuntimeEntityType(typeof(WritingSystem))?.GetTableName() ?? "WritingSystem";
                        if (!HasTable(localContext, WsTableName))
                        {
                            return [];
                        }

                        return localContext.WritingSystems.ToArray();
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
            }) ?? [];
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

    private void InvalidateWritingSystemsCache(DbConnection connection)
    {
        cache.Remove(CacheKey(connection));
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

        // Only setup basic collation - writing system collations come from EF Core path
        // Note: Collations persist on the connection, so if EF already opened this connection,
        // this is redundant but harmless. SQLite allows re-registering collations.
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
            InvalidateWritingSystemsCache(connection);
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
