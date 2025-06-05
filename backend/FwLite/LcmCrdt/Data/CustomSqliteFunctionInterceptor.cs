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

    public Task ConnectionOpenedAsync(DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = new CancellationToken())
    {
        ConnectionOpened(connection, eventData);
        return Task.CompletedTask;
    }
}
