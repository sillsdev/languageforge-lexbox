using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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

    //prevents hiding query exceptions due to Dispose throwing after the query threw an exception
    internal static async IAsyncEnumerable<T> SafeIterate<T>(IAsyncEnumerable<T> asyncEnumerable)
    {
        var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
        try
        {
            while (await asyncEnumerator.MoveNextAsync())
            {
                yield return asyncEnumerator.Current;
            }
        }
        finally
        {
            try
            {
                await asyncEnumerator.DisposeAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);//basically ignore, there's a known issue where dispose will throw but we want to ignore that since it would otherwise cover up when MoveNextAsync throws
            }
        }
    }
}
