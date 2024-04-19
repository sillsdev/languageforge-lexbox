namespace LexCore.Exceptions;

public class InvalidEmailException(string message, string address) : Exception(message)
{
    public string Address { get; } = address;
}
