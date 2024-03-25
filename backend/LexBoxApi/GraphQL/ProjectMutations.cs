﻿using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.GraphQL.CustomTypes;
using LexBoxApi.Models.Project;
using LexBoxApi.Services;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexCore.Utils;
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
    [Error<AlreadyExistsException>]
    [UseMutationConvention]
    [RefreshJwt]
    [VerifiedEmailRequired]
    public async Task<CreateProjectResponse?> CreateProject(
        LoggedInContext loggedInContext,
        IPermissionService permissionService,
        CreateProjectInput input,
        [Service] ProjectService projectService,
        [Service] EmailService emailService)
    {
        if (!loggedInContext.User.IsAdmin)
        {
            // For non-admins we always implicitly set them as the project manager
            // Only admins can create empty projects or projects for other users
            input = input with { ProjectManagerId = loggedInContext.User.Id };
        }

        if (!permissionService.HasProjectCreatePermission())
        {
            var draftProjectId = await projectService.CreateDraftProject(input);
            await emailService.SendCreateProjectRequestEmail(loggedInContext.User, input);
            return new CreateProjectResponse(draftProjectId, CreateProjectResult.Requested);
        }

        var projectId = await projectService.CreateProject(input);
        return new CreateProjectResponse(projectId, CreateProjectResult.Created);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<ProjectMembersMustBeVerified>]
    [Error<ProjectMemberInvitedByEmail>]
    [Error<AlreadyExistsException>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> AddProjectMember(IPermissionService permissionService,
        LoggedInContext loggedInContext,
        AddProjectMemberInput input,
        LexBoxDbContext dbContext,
        [Service] EmailService emailService)
    {
        permissionService.AssertCanManageProject(input.ProjectId);
        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        if (project is null) throw new NotFoundException("Project not found");
        var user = await dbContext.Users.Include(u => u.Projects).FindByEmail(input.UsernameOrEmail);
        if (user is null && input.UsernameOrEmail.Contains('@'))
        {
            var manager = loggedInContext.User;
            await emailService.SendCreateAccountEmail(input.UsernameOrEmail, input.ProjectId, input.Role, manager.Name, project.Name);
            throw new ProjectMemberInvitedByEmail("Invitation email sent");
        }
        if (user is null)
        {
            throw new NotFoundException("Username not found");
        }
        if (!user.EmailVerified) throw new ProjectMembersMustBeVerified("Member must verify email first");
        if (user.Projects.Any(p => p.ProjectId == input.ProjectId))
        {
            throw new AlreadyExistsException("User is already a member of this project");
        }

        user.UpdateCreateProjectsPermission(input.Role);
        dbContext.ProjectUsers.Add(
            new ProjectUsers { Role = input.Role, ProjectId = input.ProjectId, UserId = user.Id });
        user.UpdateUpdatedDate();
        project.UpdateUpdatedDate();
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
        IPermissionService permissionService,
        LexBoxDbContext dbContext)
    {
        permissionService.AssertCanManageProjectMemberRole(input.ProjectId, input.UserId);
        var projectUser =
            await dbContext.ProjectUsers.Include(r => r.Project).Include(r => r.User).FirstOrDefaultAsync(u =>
                u.ProjectId == input.ProjectId && u.UserId == input.UserId);
        if (projectUser is null) throw new NotFoundException("Project member not found");
        projectUser.Role = input.Role;
        projectUser.User.UpdateCreateProjectsPermission(input.Role);
        projectUser.User.UpdateUpdatedDate();
        projectUser.Project.UpdateUpdatedDate();
        projectUser.UpdateUpdatedDate();
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
        IPermissionService permissionService,
        LexBoxDbContext dbContext)
    {
        permissionService.AssertCanManageProject(input.ProjectId);
        if (input.Name.IsNullOrEmpty()) throw new RequiredException("Project name cannot be empty");

        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        if (project is null) throw new NotFoundException("Project not found");

        project.Name = input.Name;
        project.UpdateUpdatedDate();
        await dbContext.SaveChangesAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> ChangeProjectDescription(ChangeProjectDescriptionInput input,
        IPermissionService permissionService,
        LexBoxDbContext dbContext)
    {
        permissionService.AssertCanManageProject(input.ProjectId);
        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        if (project is null) throw new NotFoundException("Project not found");

        project.Description = input.Description;
        project.UpdateUpdatedDate();
        await dbContext.SaveChangesAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId);
    }

    [Error<NotFoundException>]
    [Error<LastMemberCantLeaveException>]
    [UseMutationConvention]
    [RefreshJwt]
    public async Task<Project> LeaveProject(
        Guid projectId,
        LoggedInContext loggedInContext,
        LexBoxDbContext dbContext)
    {
        var project = await dbContext.Projects.Where(p => p.Id == projectId)
            .Include(p => p.Users)
            .SingleOrDefaultAsync();
        if (project is null) throw new NotFoundException("Project not found");
        var member = project.Users.FirstOrDefault(u => u.UserId == loggedInContext.User.Id);
        if (member is null) return project;
        if (member.Role == ProjectRole.Manager && project.Users.Count(m => m.Role == ProjectRole.Manager) == 1)
        {
            throw new LastMemberCantLeaveException();
        }
        project.Users.Remove(member);
        await dbContext.SaveChangesAsync();
        return project;
    }

    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> RemoveProjectMember(RemoveProjectMemberInput input,
        IPermissionService permissionService,
        LexBoxDbContext dbContext)
    {
        permissionService.AssertCanManageProject(input.ProjectId);
        await dbContext.ProjectUsers.Where(pu => pu.ProjectId == input.ProjectId && pu.UserId == input.UserId)
            .ExecuteDeleteAsync();
        // Not doing .Include() above because we don't want the project or user removed, just the many-to-many table row
        var user = await dbContext.Users.FindAsync(input.UserId);
        if (user is not null) user.UpdateUpdatedDate();
        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        if (project is not null) project.UpdateUpdatedDate();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId);
    }


    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> SoftDeleteProject(
        Guid projectId,
        IPermissionService permissionService,
        LexBoxDbContext dbContext,
        IHgService hgService)
    {
        permissionService.AssertCanManageProject(projectId);

        var project = await dbContext.Projects.Include(p => p.Users).FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null) throw new NotFoundException("Project not found");
        if (project.DeletedDate is not null) throw new InvalidOperationException("Project already deleted");

        var deletedAt = DateTimeOffset.UtcNow;
        var timestamp = FileUtils.ToTimestamp(deletedAt);
        project.DeletedDate = deletedAt;
        project.UpdatedDate = deletedAt;
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
