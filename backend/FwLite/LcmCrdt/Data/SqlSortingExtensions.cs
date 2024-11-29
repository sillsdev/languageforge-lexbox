using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
using System.Linq.Expressions;
using LinqToDB;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SIL.WritingSystems;

namespace LcmCrdt.Data;

public static class SqlSortingExtensions
{
    public const string CollateUnicodeNoCase = "NOCASE_UNICODE";

    [ExpressionMethod(nameof(CollateUnicodeExpression))]
    internal static string CollateUnicode(this string value, WritingSystem ws)
    {
        //could optionally just return the value here, but it would work differently than sql
        throw new InvalidOperationException("CollateUnicode is server-side only API.");
    }

    private static Expression<Func<string, WritingSystem, string>> CollateUnicodeExpression()
    {
        //todo maybe in the future we use a custom collation based on the writing system
        return (s, ws) => s.Collate(CollationName(ws));
    }

    internal static string CollationName(WritingSystem ws)
    {
        //don't use ':' in the name, it won't work
        return $"NOCASE_WS_{ws.WsId}";
    }
}

public class SetupCollationInterceptor(IMemoryCache cache, ILogger<SetupCollationInterceptor> logger) : IDbConnectionInterceptor, ISaveChangesInterceptor
{
    private WritingSystem[] GetWritingSystems(LcmCrdtDbContext dbContext, DbConnection connection)
    {
        //todo this needs to be invalidated when the writing systems change
        return cache.GetOrCreate(CacheKey(connection),
            entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(30);
                try
                {

                    return dbContext.WritingSystems.ToArray();
                }
                catch (SqliteException e)
                {
                    return [];
                }
            }) ?? [];
    }

    private static string CacheKey(DbConnection connection)
    {
        return $"writingSystems|{connection.ConnectionString}";
    }

    private void InvalidateWritingSystemsCache(DbConnection connection)
    {
        cache.Remove(CacheKey(connection));
    }

    public void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        var context = (LcmCrdtDbContext?)eventData.Context;
        if (context is null) throw new InvalidOperationException("context is null");
        var sqliteConnection = (SqliteConnection)connection;
        SetupCollations(sqliteConnection, GetWritingSystems(context, connection));

        //setup general use collation
        sqliteConnection.CreateCollation(SqlSortingExtensions.CollateUnicodeNoCase,
            CultureInfo.CurrentCulture.CompareInfo,
            (compareInfo, x, y) => compareInfo.Compare(x, y, CompareOptions.IgnoreCase));
    }

    public Task ConnectionOpenedAsync(DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ConnectionOpened(connection, eventData);
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
        CompareInfo compareInfo;
        try
        {
            //todo use ICU/SLDR instead
            compareInfo = CultureInfo.CreateSpecificCulture(writingSystem.WsId.Code).CompareInfo;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create compare info for '{WritingSystemId}'", writingSystem.WsId);
            compareInfo = CultureInfo.InvariantCulture.CompareInfo;
        }
        connection.CreateCollation(SqlSortingExtensions.CollationName(writingSystem),
            //todo use custom comparison based on the writing system
            compareInfo,
            static (compareInfo, x, y) => compareInfo.Compare(x, y, CompareOptions.IgnoreCase));
    }
}
