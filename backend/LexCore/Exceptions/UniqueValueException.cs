namespace LexCore.Exceptions;

public class UniqueValueException: Exception
{
    public UniqueValueException(string field) : base($"The value for {field} is not unique")
    {
    }
}
