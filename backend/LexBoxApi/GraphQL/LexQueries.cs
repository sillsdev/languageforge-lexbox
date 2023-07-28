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
        var projectCodes = loggedInContext.User.Projects.Select(p => p.Code);
        return context.Projects.Where(p => projectCodes.Contains(p.Code));
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

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [AdminRequired]
    public IQueryable<User> Users(LexBoxDbContext context)
    {
        return context.Users;
    }

    public LexAuthUser Me(LoggedInContext loggedInContext)
    {
        return loggedInContext.User;
    }
}
