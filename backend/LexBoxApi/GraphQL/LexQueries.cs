using LexBoxApi.Auth;
using LexCore.Auth;
using LexCore.Entities;
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

    [UseSingleOrDefault]
    [UseProjection]
    public IQueryable<Project> ProjectByCode(LexBoxDbContext context, LoggedInContext loggedInContext, string code)
    {
        loggedInContext.User.AssertCanAccessProject(code);
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
}
