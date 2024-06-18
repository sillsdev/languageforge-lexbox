using HotChocolate.Resolvers;
using LexBoxApi.Auth;
using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

public class OrgMembersVisibilityMiddleware(FieldDelegate next)
{
    public async Task InvokeAsync(IMiddlewareContext context)
    {
        await next(context);
        var result = context.Result;
        if (result is List<OrgMember> members)
        {
            var user = context.Service<LoggedInContext>().MaybeUser;
            var org = context.Parent<Organization>();
            if (user is not null && org is not null && (user.IsAdmin || user.Orgs.Any(o => o.OrgId == org.Id)))
            {
                return;
            }
            else
            {
                // Non-members may only see org admins, not whole membership
                context.Result = members.Where(om => om.Role == OrgRole.Admin);
            }
        }
    }
}
