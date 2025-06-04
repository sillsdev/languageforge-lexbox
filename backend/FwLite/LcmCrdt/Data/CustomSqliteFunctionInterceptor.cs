using System.Data.Common;
using System.Globalization;
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
                    CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) >= 0;
            });
    }

    public Task ConnectionOpenedAsync(DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = new CancellationToken())
    {
        ConnectionOpened(connection, eventData);
        return Task.CompletedTask;
    }
}
