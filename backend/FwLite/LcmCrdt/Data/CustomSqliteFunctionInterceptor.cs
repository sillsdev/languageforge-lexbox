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
            RegisterContainsFunction(sqliteConnection);
        }
    }

    public static void RegisterContainsFunction(SqliteConnection sqliteConnection)
    {
        //creates a new function that can be used in queries
        sqliteConnection.CreateFunction(ContainsFunction,
            //in sqlite strings are byte arrays, so we can avoid allocating strings by using spans
            (byte[]? str, byte[]? value) =>
            {
                if (str is null || value is null) return false;

                Span<char> source = stackalloc char[Encoding.UTF8.GetCharCount(str)];
                Span<char> search = stackalloc char[Encoding.UTF8.GetCharCount(value)];
                Encoding.UTF8.GetChars(str, source);
                Encoding.UTF8.GetChars(value, search);
                return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source,
                    search,
                    ContainsDiacritic(search)
                        ? CompareOptions.IgnoreCase
                        : CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase
                ) >= 0;
            });
    }

    private static bool ContainsDiacritic(in ReadOnlySpan<char> value)
    {
        bool hasAccent = false;
        //todo we could maybe get rid of this normalization step if the text is already normalized
        //that would mean we could just iterate the value here rather than creating a new string
        foreach (var ch in new string(value).Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                hasAccent = true;
                break;
            }
        }
        return hasAccent;
    }
}
