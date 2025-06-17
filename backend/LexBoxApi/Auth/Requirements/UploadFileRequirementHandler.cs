using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.AspNetCore.Authorization;

namespace LexBoxApi.Auth.Requirements;


public class UserCanUploadMediaFilesRequirement : IAuthorizationRequirement
{
}

public class UserCanDownloadMediaFilesRequirement : IAuthorizationRequirement
{
}

public class UploadFileRequirementHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<UserCanUploadMediaFilesRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserCanUploadMediaFilesRequirement requirement)
    {
        return MediaFileRequirementHandlerImpl.HandleRequirementAsync(
            this,
            httpContextAccessor,
            context,
            requirement,
            writeAccessRequired: true
        );
    }
}

public class DownloadFileRequirementHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<UserCanDownloadMediaFilesRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserCanDownloadMediaFilesRequirement requirement)
    {
        return MediaFileRequirementHandlerImpl.HandleRequirementAsync(
            this,
            httpContextAccessor,
            context,
            requirement,
            writeAccessRequired: false
        );
    }
}

internal static class MediaFileRequirementHandlerImpl
{
    public static async Task HandleRequirementAsync(IAuthorizationHandler handler, IHttpContextAccessor httpContextAccessor, AuthorizationHandlerContext context, IAuthorizationRequirement requirement, bool writeAccessRequired)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null) return;
        var user = httpContext.RequestServices.GetRequiredService<LoggedInContext>().MaybeUser;
        if (user is null)
        {
            context.Fail(new AuthorizationFailureReason(handler, $"Must be logged in to upload media files"));
            return;
        }
        Guid projectId = default;
        if (httpContext.Request.RouteValues.TryGetValue("projectId", out var projectIdValue))
        {
            if (projectIdValue is string s && Guid.TryParse(s, out var parsed)) projectId = parsed;
        }
        if (projectId == default)
        {
            Guid fileId = default;
            if (httpContext.Request.RouteValues.TryGetValue("fileId", out var fileIdValue))
            {
                if (fileIdValue is string s && Guid.TryParse(s, out var parsed)) fileId = parsed;
            }
            var dbContext = httpContext!.RequestServices.GetRequiredService<LexBoxDbContext>();
            var file = await dbContext.Files.FindAsync(fileId);
            if (file is not null) projectId = file.ProjectId;
        }
        if (projectId == default) return;
        var permissionService = httpContext!.RequestServices.GetRequiredService<IPermissionService>();
        var hasAccess =
            writeAccessRequired
            ? await permissionService.CanSyncProject(projectId)
            : await permissionService.CanViewProject(projectId);
        if (!hasAccess)
        {
            context.Fail(new AuthorizationFailureReason(handler, $"User does not have access to project {projectId}"));
            return;
        }
        else
        {
            context.Succeed(requirement);
        }
    }
}
