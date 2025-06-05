using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MiniLcm.Culture;

namespace LcmCrdt.Data;

public class CustomSqliteFunctionInterceptor : IDbConnectionInterceptor
{
    public const string ContainsFunction = "contains";

    private record DiacriticResult(bool HasDiacritic);
    private static readonly ConditionalWeakTable<object, DiacriticResult> Cache = new();
    public void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        var sqliteConnection = (SqliteConnection)connection;
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
                    ContainsDiacritic(search, value)
                        ? CompareOptions.IgnoreCase
                        : CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase
                ) >= 0;
            });
    }

    private static bool ContainsDiacritic(in ReadOnlySpan<char> value, object resultKey)
    {
        if (Cache.TryGetValue(resultKey, out var result)) return result.HasDiacritic;
        bool hasAccent = false;
        var str = new string(value);
        //todo we could maybe get rid of this normalization step if the text is already normalized
        //that would mean we could just iterate the value here rather than creating a new string
        foreach (var ch in str.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                hasAccent = true;
                break;
            }
        }

        Cache.Add(resultKey, new DiacriticResult(hasAccent));
        return hasAccent;
    }

    public Task ConnectionOpenedAsync(DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = new CancellationToken())
    {
        ConnectionOpened(connection, eventData);
        return Task.CompletedTask;
    }
}
