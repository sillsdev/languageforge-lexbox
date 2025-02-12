using LexCore.Entities;

namespace LexCore.Exceptions;

public class OrgMembersMustBeVerifiedForRole : Exception
{
    public OrgMembersMustBeVerifiedForRole(string message, OrgRole role) : base(message)
    {
        Data["role"] = role;
    }
}
