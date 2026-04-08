using System.Data.Common;
using System.Globalization;
using System.Text;
using LinqToDB.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LcmCrdt.Data;

public class CustomSqliteFunctionInterceptor : IDbConnectionInterceptor, IConnectionInterceptor
{
    public const string ContainsFunction = "contains";
    public const string StartsWithFunction = "startsWith";

    public void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ConnectionOpened(connection);
    }

    public Task ConnectionOpenedAsync(DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ConnectionOpened(connection);
        return Task.CompletedTask;
    }

    public void ConnectionOpening(LinqToDB.Interceptors.ConnectionEventData eventData, DbConnection connection)
    {
        // We register the function after connection opens, not before
    }

    public Task ConnectionOpeningAsync(LinqToDB.Interceptors.ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void ConnectionOpened(LinqToDB.Interceptors.ConnectionEventData eventData, DbConnection connection)
    {
        ConnectionOpened(connection);
    }

    public Task ConnectionOpenedAsync(LinqToDB.Interceptors.ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
    {
        ConnectionOpened(connection);
        return Task.CompletedTask;
    }

    private void ConnectionOpened(DbConnection connection)
    {
        if (connection is SqliteConnection sqliteConnection)
        {
            RegisterCustomFunctions(sqliteConnection);
        }
    }

    public static void RegisterCustomFunctions(SqliteConnection sqliteConnection)
    {
        sqliteConnection.CreateFunction(ContainsFunction,
            (byte[]? str, byte[]? value, bool matchDiacritics) =>
            {
                if (str is null || value is null) return false;

                Span<char> source = stackalloc char[Encoding.UTF8.GetCharCount(str)];
                Span<char> search = stackalloc char[Encoding.UTF8.GetCharCount(value)];
                Encoding.UTF8.GetChars(str, source);
                Encoding.UTF8.GetChars(value, search);
                var options = DiacriticMatchOptions(matchDiacritics);
                return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, search, options) >= 0;
            });

        sqliteConnection.CreateFunction(StartsWithFunction,
            (byte[]? str, byte[]? value, bool matchDiacritics) =>
            {
                if (str is null || value is null) return false;

                Span<char> source = stackalloc char[Encoding.UTF8.GetCharCount(str)];
                Span<char> search = stackalloc char[Encoding.UTF8.GetCharCount(value)];
                Encoding.UTF8.GetChars(str, source);
                Encoding.UTF8.GetChars(value, search);
                var options = DiacriticMatchOptions(matchDiacritics);
                return CultureInfo.InvariantCulture.CompareInfo.IsPrefix(source, search, options);
            });
    }

    private static CompareOptions DiacriticMatchOptions(bool matchDiacritics)
    {
        return matchDiacritics
            ? CompareOptions.IgnoreCase
            : CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
    }
}
