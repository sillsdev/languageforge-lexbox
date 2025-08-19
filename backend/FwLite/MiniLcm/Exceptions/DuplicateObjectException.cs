namespace MiniLcm.Exceptions;

public class DuplicateObjectException : Exception
{
    public DuplicateObjectException(string? message) : base(message)
    {
    }

    public DuplicateObjectException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
