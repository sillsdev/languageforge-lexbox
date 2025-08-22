namespace MiniLcm.Exceptions;

public class SyncObjectException: Exception
{
    public SyncObjectException(string? message) : base(message)
    {
    }

    public SyncObjectException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
