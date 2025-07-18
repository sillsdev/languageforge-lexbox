using LexCore.Auth;
using LexCore.Entities;

namespace LexBoxApi.GraphQL;

public static class LexAuthUserCanDownloadProjectsExtensions
{
    public static bool CanDownloadProjectsWithoutMembership(this LexAuthUser user)
    {
        if (user.IsAdmin) return true;
        if (user.Orgs.Any(o => o.Role == OrgRole.Admin)) return true;
        return false;
    }
}
