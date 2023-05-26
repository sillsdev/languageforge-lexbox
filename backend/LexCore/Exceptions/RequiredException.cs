namespace LexCore.Exceptions;

public class RequiredException : Exception
{
    public RequiredException(string message) : base(message)
    {
    }
}
