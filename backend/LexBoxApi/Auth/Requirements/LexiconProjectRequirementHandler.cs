using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace LexBoxApi.Auth.Requirements;

public class LexiconProjectRequirement(bool writeRequired) : IAuthorizationRequirement
{
    public bool WriteAccessRequired { get; } = writeRequired;
}

/// <summary>
/// Read: anyone who can view the project. Write: Editor Lite, Editor or Manager (plus admins and org managers),
/// the same roles that may upload media files.
/// </summary>
public class LexiconProjectRequirementHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<LexiconProjectRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, LexiconProjectRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null) return;
        if (httpContext.RequestServices.GetRequiredService<LoggedInContext>().MaybeUser is null)
        {
            context.Fail(new AuthorizationFailureReason(this, "Must be logged in to use the lexicon API"));
            return;
        }
        if (!httpContext.Request.RouteValues.TryGetValue("projectId", out var projectIdValue)
            || !Guid.TryParse(projectIdValue?.ToString(), out var projectId))
        {
            context.Fail(new AuthorizationFailureReason(this, "Lexicon API routes need a project id"));
            return;
        }

        // An unknown project has nothing to protect: let FwHeadless answer 404, so a missing project isn't reported as forbidden
        var dbContext = httpContext.RequestServices.GetRequiredService<LexBoxDbContext>();
        if (!await dbContext.Projects.AnyAsync(p => p.Id == projectId))
        {
            context.Succeed(requirement);
            return;
        }

        var permissionService = httpContext.RequestServices.GetRequiredService<IPermissionService>();
        var hasAccess = requirement.WriteAccessRequired
            ? await permissionService.CanEditMediaFiles(projectId)
            : await permissionService.CanViewProject(projectId);
        if (!hasAccess)
        {
            context.Fail(new AuthorizationFailureReason(this, $"User does not have access to project {projectId}"));
            return;
        }

        context.Succeed(requirement);
    }
}
