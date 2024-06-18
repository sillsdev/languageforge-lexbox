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
            var org = context.Parent<Organization>();
            var orgId = org?.Id ?? default;
            if (orgId == default)
            {
                throw new LexCore.Exceptions.NotFoundException("Org ID not found in GraphQL - shouldn't happen", nameof(Organization));
            }
            var user = context.Service<LoggedInContext>().MaybeUser;
            if (user is null)
            {
                // Anonymous sessions can only see public projects
                context.Result = projects.Where(p => p.IsConfidential == false);
                return;
            }
            if (user.Orgs.Any(o => o.OrgId == orgId && o.Role == OrgRole.Admin))
            {
                // Org admins can see all projects
                return;
            }
            // Anyone else can only see public projects; org membership makes no difference
            context.Result = projects.Where(p => p.IsConfidential == false || user.Projects.Any(up => up.ProjectId == p.Id)).ToList();
        }
    }
}
