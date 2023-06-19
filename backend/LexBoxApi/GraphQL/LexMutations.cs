using LexBoxApi.Auth;
using LexBoxApi.Models.Project;
using LexBoxApi.Services;
using LexCore.Entities;
using LexCore.Exceptions;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
    public async Task<ProjectUsers> ChangeProjectMemberRole(ChangeProjectMemberRoleInput input,
        LexBoxDbContext dbContext)
    {
        var projectUser = await dbContext.ProjectUsers.FirstOrDefaultAsync(u => u.ProjectId == input.ProjectId && u.UserId == input.UserId);
        if (projectUser is null) throw new NotFoundException("Project member not found");
        projectUser.Role = input.Role;
        await dbContext.SaveChangesAsync();
        return projectUser;
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<RequiredException>]
    [UseMutationConvention]
    public async Task<Project> ChangeProjectName(ChangeProjectNameInput input,
        LexBoxDbContext dbContext)
    {
        if (input.Name.IsNullOrEmpty()) throw new RequiredException("Project name cannot be empty");

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

    [Error<NotFoundException>]
    [Error<DbError>]
    //[Error<RequiredException>]
    [UseMutationConvention]
    public async Task<User> ChangeUserAccountData(ChangeUserAccountDataInput input, LexBoxDbContext dbContext)
    {
        //if (input.UserId.IsNullOrEmpty()) throw new RequiredException("UserId cannot be empty");

        var user = await dbContext.Users.FindAsync(input.UserId); //find based on userId
        if (user is null) throw new NotFoundException("User not found");
        if (_loggedInContext.User.Id != input.UserId) throw new RequiredException("User id of input does not match that of logged in user.");
        // everything can change except UserId
        //minimum email = a@a.a
        if (input.Email is not null and input.Email != ""){
            if (input.Email.Contains("@") == false || input.Email.Length < 3){
                throw new RequiredException("Email does not match requirements");
            }
            user.Email = input.Email;
        }

        if (input.Name is not null and input.Name != ""){user.Name = input.Name;}

        //user.Username = input.Username;
        await dbContext.SaveChangesAsync();

        return user;
    }
}
