using LexBoxApi.Auth;
using LexBoxApi.Models.Project;
using LexBoxApi.Services;
using LexCore.Entities;
using LexData;

namespace LexBoxApi.GraphQL;

public class LexMutations
{
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly LoggedInContext _loggedInContext;
    public LexMutations(LexBoxDbContext lexBoxDbContext, LoggedInContext loggedInContext)
    {
        _lexBoxDbContext = lexBoxDbContext;
        _loggedInContext = loggedInContext;
    }

    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IExecutable<Project>> CreateProject(CreateProjectInput input, [Service] ProjectService projectService)
    {
        var projectId = await projectService.CreateProject(input, _loggedInContext.User.Id);
        return _lexBoxDbContext.Projects.Where(p => p.Id == projectId).AsExecutable();
    }
}