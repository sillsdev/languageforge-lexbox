using HotChocolate.Resolvers;
using LexBoxApi.Auth;
using LexCore.Auth;
using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

public class RefreshJwtProjectMembershipMiddleware(FieldDelegate next)
{
    private const string REFRESHED_USER_KEY = "RefreshedJwtMemberships";

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        await next(context);
        if (UserAlreadyRefreshed(context))
        {
            return;
        }

        var user = context.Service<LoggedInContext>().MaybeUser;
        if (user is null || user.Role == UserRole.admin) return;

        var projectId = context.Parent<Project>().Id;
        if (projectId == default)
        {
            if (context.Result is not Guid projectGuid) return;
            if (projectGuid == default) return;
            projectId = projectGuid;
        } // we know we have a valid project-ID

        var currUserMembershipJwt = user.Projects.FirstOrDefault(projects => projects.ProjectId == projectId);

        if (currUserMembershipJwt is null)
        {
            // The user was probably added to the project and it's not in the token yet
            await RefreshUser(context, user.Id);
            return;
        }

        if (context.Result is not IEnumerable<ProjectUsers> projectUsers) return;

        var sampleProjectUser = projectUsers.FirstOrDefault();
        if (sampleProjectUser is not null && sampleProjectUser.UserId == default && (sampleProjectUser.User == null || sampleProjectUser.User.Id == default))
        {
            // User IDs don't seem to have been loaded from the DB, so we can't do anything
            return;
        }

        var currUserMembershipDb = projectUsers.FirstOrDefault(projectUser => user.Id == projectUser.UserId || user.Id == projectUser.User?.Id);
        if (currUserMembershipDb is null)
        {
            // The user was probably removed from the project and it's still in the token
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
        await lexAuthService.RefreshUser(userId, LexAuthConstants.ProjectsClaimType);
    }

    private static bool UserAlreadyRefreshed(IMiddlewareContext context)
    {
        return context.ContextData.ContainsKey(REFRESHED_USER_KEY);
    }
}
