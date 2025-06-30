using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.AspNetCore.Authorization;

namespace LexBoxApi.Auth.Requirements;

public class MediaFilesRequirement(bool writeRequired) : IAuthorizationRequirement
{
    public bool WriteAccessRequired { get; set; } = writeRequired;
}

public class MediaFileRequirementHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<MediaFilesRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MediaFilesRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null) return;
        var user = httpContext.RequestServices.GetRequiredService<LoggedInContext>().MaybeUser;
        if (user is null)
        {
            context.Fail(new AuthorizationFailureReason(this, $"Must be logged in to upload media files"));
            return;
        }
        var projectId = await GetProjectId(httpContext.Request);

        if (projectId is null)
        {
            context.Fail(new AuthorizationFailureReason(this, $"Media files must have a project ID in order to be uploaded"));
            return;
        }
        var permissionService = httpContext.RequestServices.GetRequiredService<IPermissionService>();
        var hasAccess =
            requirement.WriteAccessRequired
                ? await permissionService.CanSyncProject(projectId.Value)
                : await permissionService.CanViewProject(projectId.Value);
        if (!hasAccess)
        {
            context.Fail(new AuthorizationFailureReason(this, $"User does not have access to project {projectId.Value}"));
            return;
        }

        context.Succeed(requirement);
    }

    private static async ValueTask<Guid?> GetProjectId(HttpRequest request)
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

        if (projectId is null && GetFileId(request) is {} fileId)
        {
            var dbContext = request.HttpContext.RequestServices.GetRequiredService<LexBoxDbContext>();
            var file = await dbContext.Files.FindAsync(fileId);
            projectId = file?.ProjectId;
        }
        return projectId;
    }

    private static Guid? GetFileId(HttpRequest request)
    {
        Guid? fileId = null;
        if (request.RouteValues.TryGetValue("fileId", out var fileIdValue))
        {
            if (fileIdValue is string s && Guid.TryParse(s, out var parsed)) fileId = parsed;
        }
        return fileId;
    }
}
