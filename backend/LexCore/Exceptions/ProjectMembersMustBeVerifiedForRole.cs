using LexCore.Entities;

namespace LexCore.Exceptions;

public class ProjectMembersMustBeVerifiedForRole : Exception
{
    public ProjectMembersMustBeVerifiedForRole(string message, ProjectRole role) : base(message)
    {
        Data["role"] = role;
    }
}
