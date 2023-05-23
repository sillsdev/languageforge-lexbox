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

    [Error<DbError>]
    [UseMutationConvention]
    public async Task<Project?> CreateProject(CreateProjectInput input,
        [Service] ProjectService projectService,
        LexBoxDbContext dbContext)
    {
        var projectId = await projectService.CreateProject(input, _loggedInContext.User.Id);
        return await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    public async Task<Project> AddProjectMember(AddProjectMemberInput input,
        LexBoxDbContext dbContext)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u =>
            u.Username == input.UserEmail || u.Email == input.UserEmail);
        if (user is null) throw new NotFoundException("Member not found");
        dbContext.ProjectUsers.Add(
            new ProjectUsers { Role = input.Role, ProjectId = input.ProjectId, UserId = user.Id });
        await dbContext.SaveChangesAsync();
        return await dbContext.Projects.Where(p => p.Id == input.ProjectId).FirstAsync();
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    public async Task<Project> ChangeProjectName(ChangeProjectNameInput input,
        LexBoxDbContext dbContext)
    {
        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        if (project is null) throw new NotFoundException("Project not found");

        project.Name = input.Name;
        await dbContext.SaveChangesAsync();
        return project;
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    public async Task<Project> ChangeProjectDescription(ChangeProjectDescriptionInput input,
        LexBoxDbContext dbContext)
    {
        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        if (project is null) throw new NotFoundException("Project not found");

        project.Description = input.Description;
        await dbContext.SaveChangesAsync();
        return project;
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