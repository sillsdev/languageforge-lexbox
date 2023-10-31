namespace LexCore.Exceptions;

public interface IExceptionWithCode
{
    string Code { get; }
}

public class ExceptionWithCode : Exception, IExceptionWithCode
{
    public ExceptionWithCode(string message, string code) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
