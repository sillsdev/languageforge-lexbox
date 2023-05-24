using LexBoxApi.Auth;
using LexBoxApi.Services;
using LexCore.Auth;
using LexCore.Entities;
using LexData;

namespace LexBoxApi.GraphQL;

public class LexQueries
{
    private readonly LoggedInContext _loggedInContext;

    public LexQueries(LoggedInContext loggedInContext)
    {
        _loggedInContext = loggedInContext;
    }

    [UseProjection]
    public IQueryable<Project> MyProjects(LexBoxDbContext context)
    {
        var projectCodes = _loggedInContext.User.Projects.Select(p => p.Code);
        return context.Projects.Where(p => projectCodes.Contains(p.Code));
    }

    public LexAuthUser Me()
    {
        return _loggedInContext.User;
    }
    
    public Task<Changeset[]> Changesets([Service] IHgService hgService, string projectCode)
    {
        return hgService.GetChangesets(projectCode);
    }
}

