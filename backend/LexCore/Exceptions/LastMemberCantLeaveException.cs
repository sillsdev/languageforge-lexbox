namespace LexCore.Exceptions;

public class LastMemberCantLeaveException : Exception
{
    public LastMemberCantLeaveException() : base("The last member of a project can't leave the project.")
    {
    }
}
