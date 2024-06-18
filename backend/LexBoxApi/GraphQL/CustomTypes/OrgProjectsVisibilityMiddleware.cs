using HotChocolate.Resolvers;
using LexBoxApi.Auth;
using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

public class OrgProjectsVisibilityMiddleware(FieldDelegate next)
{
    public async Task InvokeAsync(IMiddlewareContext context)
    {
        await next(context);
        if (context.Result is List<Project> projects)
        {
            var user = context.Service<LoggedInContext>().MaybeUser;
            if (user is null)
            {
                // Anonymous sessions can only see public projects
                context.Result = projects.Where(p => p.IsConfidential == false);
                return;
            }
            var org = context.Parent<Organization>();
            if (org is not null && user.Orgs.Any(o => o.OrgId == org.Id && o.Role == OrgRole.Admin))
            {
                // Org admins can see all projects
                return;
            }
            // Anyone else can only see public projects or projects they themselves are a member of; org membership makes no difference
            context.Result = projects.Where(p => p.IsConfidential == false || user.Projects.Any(up => up.ProjectId == p.Id)).ToList();
        }
    }
}
