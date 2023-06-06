using LexCore.Auth;
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

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        UserHasAccessToProjectRequirement requirement)
    {
        var user = LexAuthUser.FromClaimsPrincipal(context.User);
        if (user is null || _httpContextAccessor.HttpContext is null)
        {
            return Task.CompletedTask;
        }

        var projectCode = _httpContextAccessor.HttpContext.Request.GetProjectCode();
        if (string.IsNullOrEmpty(projectCode))
        {
            context.Fail(new AuthorizationFailureReason(this, "No repoId query parameter"));
            return Task.CompletedTask;
        }

        if (user.Role == UserRole.admin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var userProject = user.Projects.FirstOrDefault(p => p.Code == projectCode);
        if (userProject is null)
        {
            context.Fail(new AuthorizationFailureReason(this, $"User does not have access to project {projectCode}"));
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}