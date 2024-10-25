using HotChocolate.Resolvers;
using LexBoxApi.Auth;
using LexBoxApi.Services;
using LexCore.Auth;
using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

public class ProjectConfidentialityCachingMiddleware(FieldDelegate next)
{
    public async Task InvokeAsync(IMiddlewareContext context, ProjectService projectService, LoggedInContext loggedInContext)
    {
        await next(context);

        if (loggedInContext.MaybeUser is { Role: UserRole.admin }) return;

        var project = context.Parent<Project>();
        var projectId = project.Id;
        if (projectId == default) return;
        var isConfidential = project.IsConfidential;
        projectService.UpdateProjectConfidentialityCache(projectId, isConfidential);
    }
}
