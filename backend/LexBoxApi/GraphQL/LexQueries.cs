using LexBoxApi.Auth;
using LexBoxApi.Services;
using LexCore.Auth;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;

namespace LexBoxApi.GraphQL;

[QueryType]
public class LexQueries
{
    private readonly LoggedInContext _loggedInContext;

    public LexQueries(LoggedInContext loggedInContext)
    {
        _loggedInContext = loggedInContext;
    }

    [UseProjection]
    [UseSorting]
    public IQueryable<Project> MyProjects(LexBoxDbContext context)
    {
        var projectCodes = _loggedInContext.User.Projects.Select(p => p.Code);
        return context.Projects.Where(p => projectCodes.Contains(p.Code));
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [AdminRequired]
    public IQueryable<Project> Projects(LexBoxDbContext context)
    {
        return context.Projects;
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [AdminRequired]
    public IQueryable<User> Users(LexBoxDbContext context)
    {
        return context.Users;
    }

    public LexAuthUser Me()
    {
        return _loggedInContext.User;
    }
}
