using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexCore.Auth;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.GraphQL;

[QueryType]
public class LexQueries
{

    [UseProjection]
    [UseSorting]
    public async Task<IQueryable<Project>> MyProjects(LoggedInContext loggedInContext, LexBoxDbContext context, LexAuthService lexAuthService)
    {
        var userId = loggedInContext.User.Id;
        var projects = context.Projects.Where(p => p.Users.Select(u => u.UserId).Contains(userId));
        if (loggedInContext.User.Role != UserRole.admin && !ProjectsMatch(projects, loggedInContext.User.Projects))
        {
            await lexAuthService.RefreshUser(userId, LexAuthConstants.ProjectsClaimType);
        }
        return projects;
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [AdminRequired]
    public IQueryable<Project> Projects(LexBoxDbContext context, bool withDeleted = false)
    {
        if (withDeleted)
        {
            return context.Projects.IgnoreQueryFilters();
        }
        else
        {
            return context.Projects;
        }
    }

    [UseSingleOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> ProjectByCode(LexBoxDbContext context, IPermissionService permissionService, string code)
    {
        await permissionService.AssertCanAccessProject(code);
        return context.Projects.Where(p => p.Code == code);
    }

    [UseOffsetPaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [AdminRequired]
    public IQueryable<User> Users(LexBoxDbContext context)
    {
        //default order by, can be overwritten by the gql query
        return context.Users.OrderBy(u => u.Name);
    }

    public LexAuthUser Me(LoggedInContext loggedInContext)
    {
        return loggedInContext.User;
    }

    private static bool ProjectsMatch(IQueryable<Project> dbProjects, AuthUserProject[] jwtProjects)
    {
        if (dbProjects.Count() != jwtProjects.Length) return false;
        var dbProjectIds = dbProjects.Select(p => p.Id).ToHashSet();
        return jwtProjects.All(p => dbProjectIds.Contains(p.ProjectId));
    }
}
