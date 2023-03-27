using LexBoxApi.Auth;
using LexBoxApi.Models.Project;
using LexBoxApi.Services;
using LexCore.Entities;
using LexCore.Exceptions;
using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.GraphQL;

public class LexMutations
{
    private readonly LoggedInContext _loggedInContext;

    public LexMutations(LoggedInContext loggedInContext)
    {
        _loggedInContext = loggedInContext;
    }

    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IExecutable<Project>> CreateProject(CreateProjectInput input,
        [Service] ProjectService projectService,
        LexBoxDbContext dbContext)
    {
        var projectId = await projectService.CreateProject(input, _loggedInContext.User.Id);
        return dbContext.Projects.Where(p => p.Id == projectId).AsExecutable();
    }

    [UseFirstOrDefault]
    [UseProjection]
    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    public async Task<IExecutable<Project>> AddProjectMember(AddProjectMemberInput input,
        LexBoxDbContext dbContext)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u =>
            u.Username == input.UserEmail || u.Email == input.UserEmail);
        if (user is null) throw new NotFoundException("User not found");
        dbContext.ProjectUsers.Add(
            new ProjectUsers { Role = input.Role, ProjectId = input.ProjectId, UserId = user.Id });
        await dbContext.SaveChangesAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId).AsExecutable();
    }

    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IExecutable<Project>> RemoveProjectMember(RemoveProjectMemberInput input,
        LexBoxDbContext dbContext)
    {
        await dbContext.ProjectUsers.Where(pu => pu.ProjectId == input.ProjectId && pu.UserId == input.UserId)
            .ExecuteDeleteAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId).AsExecutable();
    }
}