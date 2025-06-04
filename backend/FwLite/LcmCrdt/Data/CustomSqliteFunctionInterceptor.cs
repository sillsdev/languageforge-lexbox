using System.Data.Common;
using System.Globalization;
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
            (string? str, string? value) =>
            {
                if (str is null || value is null) return false;
                return str.Contains(value,
                    CultureInfo.InvariantCulture,
                    CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase);
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
