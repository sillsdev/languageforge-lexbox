using HotChocolate.Resolvers;
using LexBoxApi.Auth;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;

namespace LexBoxApi.GraphQL.CustomTypes;

public class ProjectMembersVisibilityMiddleware(FieldDelegate next)
{
    public async Task InvokeAsync(IMiddlewareContext context, IPermissionService permissionService, LoggedInContext loggedInContext)
    {
        await next(context);
        if (context.Result is IEnumerable<ProjectUsers> projectUsers)
        {
            var contextProject = context.Parent<Project>();
            var projId = contextProject?.Id ?? throw new RequiredException("Must include project ID in query if querying users");
            if (!await permissionService.CanViewProjectMembers(projId))
            {
                // Confidential project, and user doesn't have permission to see its users, so only show the current user's membership
                context.Result = projectUsers.Where(pu => pu.User?.Id == loggedInContext.MaybeUser?.Id).ToList();
            }
        }
    }
}
