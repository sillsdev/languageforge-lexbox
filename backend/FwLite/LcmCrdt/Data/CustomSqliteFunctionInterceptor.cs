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
            (byte[]? str, byte[]? value) =>
            {
                if (str is null || value is null) return false;

                Span<char> source = stackalloc char[Encoding.UTF8.GetCharCount(str)];
                Span<char> search = stackalloc char[Encoding.UTF8.GetCharCount(value)];
                Encoding.UTF8.GetChars(str, source);
                Encoding.UTF8.GetChars(value, search);
                var options = DiacriticMatchOptions(search);
                return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, search, options) >= 0;
            });

        sqliteConnection.CreateFunction(StartsWithFunction,
            (byte[]? str, byte[]? value) =>
            {
                if (str is null || value is null) return false;

                Span<char> source = stackalloc char[Encoding.UTF8.GetCharCount(str)];
                Span<char> search = stackalloc char[Encoding.UTF8.GetCharCount(value)];
                Encoding.UTF8.GetChars(str, source);
                Encoding.UTF8.GetChars(value, search);
                var options = DiacriticMatchOptions(search);
                return CultureInfo.InvariantCulture.CompareInfo.IsPrefix(source, search, options);
            });
    }

    private static CompareOptions DiacriticMatchOptions(in ReadOnlySpan<char> search)
    {
        return ContainsDiacritic(search)
            ? CompareOptions.IgnoreCase
            : CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
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
