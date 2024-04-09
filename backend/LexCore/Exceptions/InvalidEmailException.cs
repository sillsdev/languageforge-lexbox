namespace LexCore.Exceptions;

public class InvalidEmailException: Exception
{
    public InvalidEmailException(string message, string address) : base(message)
    {
        Data["address"] = address;
    }
}
