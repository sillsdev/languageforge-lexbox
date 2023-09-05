using LexBoxApi.Auth;
using LexBoxApi.GraphQL.CustomTypes;
using LexBoxApi.Models.Project;
using LexBoxApi.Services;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexData;
using LexData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LexBoxApi.GraphQL;

[MutationType]
public class ProjectMutations
{
    public enum CreateProjectResult
    {
        Created,
        Requested
    }

    public record CreateProjectResponse(Guid? Id, CreateProjectResult Result);
    [Error<DbError>]
    [UseMutationConvention]
    [RefreshJwt]
    public async Task<CreateProjectResponse?> CreateProject(
        LoggedInContext loggedInContext,
        CreateProjectInput input,
        [Service] ProjectService projectService,
        LexBoxDbContext dbContext)
    {
        if (!loggedInContext.User.HasProjectCreatePermission())
        {
            //todo send email to admin
            return new CreateProjectResponse(null, CreateProjectResult.Requested);
        }

        var projectId = await projectService.CreateProject(input, loggedInContext.User.Id);
        return new CreateProjectResponse(projectId, CreateProjectResult.Created);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> AddProjectMember(LoggedInContext loggedInContext,
        AddProjectMemberInput input,
        LexBoxDbContext dbContext)
    {
        loggedInContext.User.AssertCanManageProject(input.ProjectId);
        var user = await dbContext.Users.FindByEmail(input.UserEmail);
        if (user is null) throw new NotFoundException("Member not found");
        user.UpdateCreateProjectsPermission(input.Role);
        dbContext.ProjectUsers.Add(
            new ProjectUsers { Role = input.Role, ProjectId = input.ProjectId, UserId = user.Id });
        await dbContext.SaveChangesAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<ProjectUsers>> ChangeProjectMemberRole(
        ChangeProjectMemberRoleInput input,
        LoggedInContext loggedInContext,
        LexBoxDbContext dbContext)
    {
        loggedInContext.User.AssertCanManagerProjectMemberRole(input.ProjectId, input.UserId);
        var projectUser =
            await dbContext.ProjectUsers.Include(r => r.User).FirstOrDefaultAsync(u =>
                u.ProjectId == input.ProjectId && u.UserId == input.UserId);
        if (projectUser is null) throw new NotFoundException("Project member not found");
        projectUser.Role = input.Role;
        projectUser.User.UpdateCreateProjectsPermission(input.Role);
        await dbContext.SaveChangesAsync();

        return dbContext.ProjectUsers.Where(u => u.Id == projectUser.Id);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<RequiredException>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> ChangeProjectName(ChangeProjectNameInput input,
        LoggedInContext loggedInContext,
        LexBoxDbContext dbContext)
    {
        loggedInContext.User.AssertCanManageProject(input.ProjectId);
        if (input.Name.IsNullOrEmpty()) throw new RequiredException("Project name cannot be empty");

        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        if (project is null) throw new NotFoundException("Project not found");

        project.Name = input.Name;
        await dbContext.SaveChangesAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> ChangeProjectDescription(ChangeProjectDescriptionInput input,
        LoggedInContext loggedInContext,
        LexBoxDbContext dbContext)
    {
        loggedInContext.User.AssertCanManageProject(input.ProjectId);
        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        if (project is null) throw new NotFoundException("Project not found");

        project.Description = input.Description;
        await dbContext.SaveChangesAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId);
    }

    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> RemoveProjectMember(RemoveProjectMemberInput input,
        LoggedInContext loggedInContext,
        LexBoxDbContext dbContext)
    {
        loggedInContext.User.AssertCanManageProject(input.ProjectId);
        await dbContext.ProjectUsers.Where(pu => pu.ProjectId == input.ProjectId && pu.UserId == input.UserId)
            .ExecuteDeleteAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId);
    }


    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> SoftDeleteProject(
        Guid projectId,
        LoggedInContext loggedInContext,
        LexBoxDbContext dbContext,
        IHgService hgService)
    {
        loggedInContext.User.AssertCanManageProject(projectId);

        var project = await dbContext.Projects.Include(p => p.Users).FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null) throw new NotFoundException("Project not found");
        if (project.DeletedDate is not null) throw new InvalidOperationException("Project already deleted");

        var deletedAt = DateTimeOffset.UtcNow;
        var timestamp = deletedAt.ToString("yyyy_MM_dd_HHmmss");
        project.DeletedDate = deletedAt;
        var projectCode = project.Code;
        project.Code = $"{project.Code}__{timestamp}";
        project.Users.Clear();

        using var transaction = await dbContext.Database.BeginTransactionAsync();
        await dbContext.SaveChangesAsync();
        await hgService.SoftDeleteRepo(projectCode, timestamp);
        await transaction.CommitAsync();

        return dbContext.Projects.Where(p => p.Id == projectId);
    }
}
