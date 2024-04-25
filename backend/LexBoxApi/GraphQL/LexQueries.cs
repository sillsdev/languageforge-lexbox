using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.GraphQL.CustomTypes;
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
    public IQueryable<Project> MyProjects(LoggedInContext loggedInContext, LexBoxDbContext context)
    {
        var userId = loggedInContext.User.Id;
        return context.Projects.Where(p => p.Users.Select(u => u.UserId).Contains(userId));
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

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [AdminRequired]
    public IQueryable<DraftProject> DraftProjects(LexBoxDbContext context)
    {
        return context.DraftProjects;
    }

    [UseSingleOrDefault]
    [UseProjection]
    public IQueryable<Project> ProjectById(LexBoxDbContext context, IPermissionService permissionService, Guid projectId)
    {
        permissionService.AssertCanAccessProject(projectId);
        return context.Projects.Where(p => p.Id == projectId);
    }

    [UseSingleOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> ProjectByCode(LexBoxDbContext context, IPermissionService permissionService, string code)
    {
        await permissionService.AssertCanAccessProject(code);
        return context.Projects.Where(p => p.Code == code);
    }

    [UseSingleOrDefault]
    [UseProjection]
    [AdminRequired]
    public IQueryable<DraftProject> DraftProjectByCode(LexBoxDbContext context, IPermissionService permissionService, string code)
    {
        return context.DraftProjects.Where(p => p.Code == code);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Organization> Orgs(LexBoxDbContext context)
    {
        return context.Orgs;
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Organization> MyOrgs(LexBoxDbContext context, LoggedInContext loggedInContext)
    {
        var userId = loggedInContext.User.Id;
        return context.Orgs.Where(o => o.Members.Any(m => m.UserId == userId));
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

    public async Task<MeDto?> Me(LexBoxDbContext context, LoggedInContext loggedInContext)
    {
        var userId = loggedInContext.User.Id;
        var user = await context.Users.FindAsync(userId);
        if (user == null) return null;
        return new MeDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Locale = user.LocalizationCode
        };
    }

    public LexAuthUser MeAuth(LoggedInContext loggedInContext)
    {
        return loggedInContext.User;
    }

    public LexAuthUser TestingThrowsError()
    {
        throw new Exception("This is a test error");
    }
}
