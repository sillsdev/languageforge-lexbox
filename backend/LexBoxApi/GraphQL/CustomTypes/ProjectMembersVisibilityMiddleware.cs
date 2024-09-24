using HotChocolate.Resolvers;
using LexCore.Entities;
using LexCore.ServiceInterfaces;

namespace LexBoxApi.GraphQL.CustomTypes;

public class ProjectMembersVisibilityMiddleware(FieldDelegate next)
{
    public async Task InvokeAsync(IMiddlewareContext context, IPermissionService permissionService)
    {
        await next(context);
        if (context.Result is List<ProjectUsers>)
        {
            var contextProject = context.Parent<Project>();
            var projId = contextProject?.Id;
            // If we don't have a project ID to use, have to assume it's confidential
            if (projId is null || !await permissionService.CanViewProjectMembers(projId.Value))
            {
                // Confidential project, and user doesn't have permission to see its users, so hide the users list
                context.Result = new List<ProjectUsers>();
            }
        }
    }
}
