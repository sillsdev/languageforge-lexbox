using Humanizer;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LexData;

public enum DbErrorCode
{
    Unknown,
    Duplicate,
}

public class DbError
{
    public static DbError CreateErrorFrom(NpgsqlException exception)
    {
        return new DbError(exception.Message);
    }

    public static DbError CreateErrorFrom(PostgresException exception)
    {
        return exception.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => new DbError($"{exception.TableName.Humanize().Singularize(false)} already exists", DbErrorCode.Duplicate),
            _ => new DbError(exception.Message)
        };
    }

    public static DbError CreateErrorFrom(DbUpdateException exception)
    {
        return exception.InnerException switch
        {
            PostgresException postgresException => CreateErrorFrom(postgresException),
            NpgsqlException npgsqlException => CreateErrorFrom(npgsqlException),
            _ => new DbError(exception.Message)
        };
    }

    public DbError(string message, DbErrorCode code = DbErrorCode.Unknown)
    {
        Message = message;
        Code = code;
    }

    public string Message { get; }
    public DbErrorCode Code { get; }
}