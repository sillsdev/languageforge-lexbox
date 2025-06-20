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
    public static Guid? GetProjectId(HttpRequest request)
    {
        Guid? projectId = null;
        if (request.RouteValues.TryGetValue("projectId", out var projectIdValue))
        {
            if (projectIdValue is string s && Guid.TryParse(s, out var parsed)) projectId = parsed;
        }
        if (projectId is null)
        {
            if (request.Query.TryGetValue("projectId", out var projectIdValueFromQuery))
            {
                if (projectIdValueFromQuery.FirstOrDefault() is string sq && Guid.TryParse(sq, out var parsed)) projectId = parsed;
            }
        }
        return projectId;
    }

    public static Guid? GetFileId(HttpRequest request)
    {
        Guid? fileId = null;
        if (request.RouteValues.TryGetValue("fileId", out var fileIdValue))
        {
            if (fileIdValue is string s && Guid.TryParse(s, out var parsed)) fileId = parsed;
        }
        return fileId;
    }

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
        var projectId = GetProjectId(httpContext.Request);
        if (projectId is null)
        {
            var fileId = GetFileId(httpContext.Request);
            if (fileId is not null)
            {
                var dbContext = httpContext!.RequestServices.GetRequiredService<LexBoxDbContext>();
                var file = await dbContext.Files.FindAsync(fileId);
                if (file is not null) projectId = file.ProjectId;
            }
        }
        if (projectId is null)
        {
            context.Fail(new AuthorizationFailureReason(handler, $"Media files must have a project ID in order to be uploaded"));
            return;
        }
        var permissionService = httpContext!.RequestServices.GetRequiredService<IPermissionService>();
        var hasAccess =
            writeAccessRequired
            ? await permissionService.CanSyncProject(projectId.Value)
            : await permissionService.CanViewProject(projectId.Value);
        if (!hasAccess)
        {
            context.Fail(new AuthorizationFailureReason(handler, $"User does not have access to project {projectId.Value}"));
            return;
        }
        else
        {
            context.Succeed(requirement);
        }
    }
}
