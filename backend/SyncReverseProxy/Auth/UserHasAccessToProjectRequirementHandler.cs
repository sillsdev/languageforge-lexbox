using LexCore.Auth;
using LexCore.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;

namespace LexSyncReverseProxy.Auth;

public class UserHasAccessToProjectRequirement : IAuthorizationRequirement
{
}

public class UserHasAccessToProjectRequirementHandler : AuthorizationHandler<UserHasAccessToProjectRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserHasAccessToProjectRequirementHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        UserHasAccessToProjectRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated is not true || _httpContextAccessor.HttpContext is null)
        {
            return;
        }

        var projectCode = _httpContextAccessor.HttpContext.Request.GetProjectCode();
        if (string.IsNullOrEmpty(projectCode))
        {
            context.Fail(new AuthorizationFailureReason(this, "No repoId query parameter"));
            return;
        }

        var permissionService = _httpContextAccessor.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        if (!await permissionService.CanAccessProject(projectCode))
        {
            context.Fail(new AuthorizationFailureReason(this, $"User does not have access to project {projectCode}"));
            return;
        }

        context.Succeed(requirement);
    }
}
