using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LcmCrdt.Utils;

public static class EfExtensions
{
    public static bool CausedByUniqueConstraintViolation(this DbUpdateException e)
    {
        //19 is a unique constraint violation per https://sqlite.org/rescode.html#constraint
        if (e.InnerException is SqliteException { SqliteErrorCode: 19 }) return true;
        if (e.InnerException is DbUpdateException innerException)
        {
            return innerException.CausedByUniqueConstraintViolation();
        }
        return false;
    }

    /// <summary>
    /// Runs several queries against one snapshot of the database. Deferred, so it doesn't take the write lock:
    /// in WAL mode writers carry on, in rollback-journal mode a writer waits until it ends.
    /// Inside an existing transaction the queries just run in that.
    /// </summary>
    public static async Task<T> InReadTransaction<T>(this DatabaseFacade database, Func<Task<T>> read)
    {
        if (database.CurrentTransaction is not null) return await read();
        await database.OpenConnectionAsync();
        try
        {
            //not database.BeginTransaction: that issues BEGIN IMMEDIATE, which takes the write lock
            await using var transaction = ((SqliteConnection)database.GetDbConnection()).BeginTransaction(deferred: true);
            await database.UseTransactionAsync(transaction);
            try
            {
                return await read();
            }
            finally
            {
                await database.UseTransactionAsync(null);
                //UseTransaction opened the connection once more
                await database.CloseConnectionAsync();
            }
        }
        finally
        {
            await database.CloseConnectionAsync();
        }
    }
}
