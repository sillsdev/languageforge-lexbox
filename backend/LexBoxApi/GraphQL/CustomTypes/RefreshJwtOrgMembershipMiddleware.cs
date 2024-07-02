using HotChocolate.Resolvers;
using LexBoxApi.Auth;
using LexCore.Auth;
using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

public class RefreshJwtOrgMembershipMiddleware(FieldDelegate next)
{
    private const string REFRESHED_USER_KEY = "RefreshedJwtMemberships";
    // Should be the same value as in RefreshJwtProjectMembershipMiddleware.
    public async Task InvokeAsync(IMiddlewareContext context)
    {
        await next(context);
        if (UserAlreadyRefreshed(context))
        {
            return;
        }

        var user = context.Service<LoggedInContext>().MaybeUser;
        if (user is null || user.Role == UserRole.admin) return;

        var orgId = context.Parent<Organization>().Id;
        if (orgId == default)
        {
            if (context.Result is not Guid orgGuid) return;
            if (orgGuid == default) return;
            orgId = orgGuid;
        } // we know we have a valid org-ID

        var currUserMembershipJwt = user.Orgs.FirstOrDefault(orgs => orgs.OrgId == orgId);

        if (currUserMembershipJwt is null)
        {
            // The user was probably added to the org and it's not in the token yet
            await RefreshUser(context, user.Id);
            return;
        }

        if (context.Result is not IEnumerable<OrgMember> orgMembers) return;

        var sampleOrgUser = orgMembers.FirstOrDefault();
        if (sampleOrgUser is not null && sampleOrgUser.UserId == default && (sampleOrgUser.User == null || sampleOrgUser.User.Id == default))
        {
            // User IDs don't seem to have been loaded from the DB, so we can't do anything
            return;
        }

        var currUserMembershipDb = orgMembers.FirstOrDefault(orgUser => user.Id == orgUser.UserId || user.Id == orgUser.User?.Id);
        if (currUserMembershipDb is null)
        {
            // The user was probably removed from the org and it's still in the token
            await RefreshUser(context, user.Id);
        }
        else if (currUserMembershipDb.Role == default)
        {
            return; // Either the role wasn't loaded by the query (so we can't do anything) or the role is actually Unknown which means it definitely has never been changed
        }
        else if (currUserMembershipDb.Role != currUserMembershipJwt.Role)
        {
            // The user's role was changed
            await RefreshUser(context, user.Id);
        }
    }

    private static async Task RefreshUser(IMiddlewareContext context, Guid userId)
    {
        var lexAuthService = context.Service<LexAuthService>();
        context.ContextData[REFRESHED_USER_KEY] = true;
        await lexAuthService.RefreshUser(userId, LexAuthConstants.OrgsClaimType);
    }

    private static bool UserAlreadyRefreshed(IMiddlewareContext context)
    {
        return context.ContextData.ContainsKey(REFRESHED_USER_KEY);
    }
}
