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
}
