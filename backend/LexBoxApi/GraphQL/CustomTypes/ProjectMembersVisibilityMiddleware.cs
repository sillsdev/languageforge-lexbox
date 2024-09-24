using HotChocolate.Resolvers;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;

namespace LexBoxApi.GraphQL.CustomTypes;

public class ProjectMembersVisibilityMiddleware(FieldDelegate next)
{
    public async Task InvokeAsync(IMiddlewareContext context, IPermissionService permissionService)
    {
        await next(context);
        if (context.Result is IEnumerable<ProjectUsers>)
        {
            var contextProject = context.Parent<Project>();
            var projId = contextProject?.Id ?? throw new RequiredException("Must include project ID in query if querying users");
            if (!await permissionService.CanViewProjectMembers(projId))
            {
                // Confidential project, and user doesn't have permission to see its users, so hide the users list
                context.Result = new List<ProjectUsers>();
            }
        }
    }
}
