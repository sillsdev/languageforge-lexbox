namespace LexCore.Exceptions;

public class LastMemberCantLeaveException : Exception
{
    public LastMemberCantLeaveException() : base("The last member can't leave.")
    {
    }
}
