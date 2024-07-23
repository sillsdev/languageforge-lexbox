using HotChocolate.Resolvers;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;

namespace LexBoxApi.Auth.Requirements;

public class AccessProjectUsersRequirement : IAuthorizationRequirement;

public class AccessProjectUsersRequirementHandler(
    IPermissionService permissions,
    ILogger<AccessProjectUsersRequirementHandler> logger
) : AuthorizationHandler<AccessProjectUsersRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        AccessProjectUsersRequirement requirement)
    {
        Guid projectId = Guid.Empty;
        if (context.Resource is IMiddlewareContext middlewareContext)
        {
            projectId = middlewareContext.Parent<Project>().Id;
        }
        if (projectId != Guid.Empty && await permissions.CanSyncProjectAsync(projectId))
        {
            context.Succeed(requirement);
        } else
        {
            if (projectId == Guid.Empty)
            {
                logger.LogInformation("unable to determine project id, context resource is {Type}", context.Resource?.GetType().Name ?? "null");
            }
            context.Fail(new AuthorizationFailureReason(this, "User does not have permission to access project users"));
        }
    }
}

