namespace MiniLcm.Exceptions;

public class CreateObjectException: Exception
{
    public CreateObjectException(string? message) : base(message)
    {
    }

    public CreateObjectException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
