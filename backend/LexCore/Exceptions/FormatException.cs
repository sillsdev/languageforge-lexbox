namespace LexCore.Exceptions;

public class InvalidFormatException : Exception
{
    public InvalidFormatException(string field) : base($"Invalid {field}.")
    {
    }

    public static InvalidFormatException Email()
    {
        return new InvalidFormatException(nameof(Email));
    }
}
