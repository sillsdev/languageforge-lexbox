using HotChocolate.Resolvers;
using LexBoxApi.Auth;
using LexCore.Auth;
using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

public class RefreshJwtProjectMembershipMiddleware(FieldDelegate next, ILogger<RefreshJwtProjectMembershipMiddleware> logger)
{
    public async Task InvokeAsync(IMiddlewareContext context)
    {
        await next(context);
        var httpContext = context.GetGlobalStateOrDefault<HttpContext>("HttpContext");
        if (httpContext?.Response.Headers.ContainsKey(LexAuthService.JwtUpdatedHeader) == true)
        {
            // The JWT was already updated, skip processing
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

        var lexAuthService = context.Service<LexAuthService>();
        var currUserMembershipJwt = user.Projects.FirstOrDefault(projects => projects.ProjectId == projectId);

        if (currUserMembershipJwt is null)
        {
            // The user was probably added to the project and it's not in the token yet
            await lexAuthService.RefreshUser(user.Id, LexAuthConstants.ProjectsClaimType);
            return;
        }

        if (context.Result is not IEnumerable<ProjectUsers> projectUsers) return;

        var sampleProjectUser = projectUsers.FirstOrDefault();
        if (sampleProjectUser is not null && sampleProjectUser.UserId == default && (sampleProjectUser.User == null || sampleProjectUser.User.Id == default))
        {
            // User IDs don't seem to have been loaded from the DB, so we can't do anything
            return;
        }

        var currUserMembershipDb = projectUsers.FirstOrDefault(projectUser => user.Id == projectUser.UserId || user.Id == projectUser.User.Id);
        if (currUserMembershipDb is null)
        {
            // The user was probably removed from the project and it's still in the token
            await lexAuthService.RefreshUser(user.Id, LexAuthConstants.ProjectsClaimType);
        }
        else if (currUserMembershipDb.Role == default)
        {
            return; // Either the role wasn't loaded by the query (so we can't do anything) or the role is actually Unknown which means it definitely has never been changed
        }
        else if (currUserMembershipDb.Role != currUserMembershipJwt.Role)
        {
            // The user's role was changed
            await lexAuthService.RefreshUser(user.Id, LexAuthConstants.ProjectsClaimType);
        }
    }
}
