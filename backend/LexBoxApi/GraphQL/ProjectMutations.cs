﻿using LexBoxApi.Auth;
using LexBoxApi.GraphQL.CustomTypes;
using LexBoxApi.Models.Project;
using LexBoxApi.Services;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LexBoxApi.GraphQL;

[MutationType]
public class ProjectMutations
{
    [Error<DbError>]
    [UseMutationConvention]
    [RefreshJwt]
    public async Task<Project?> CreateProject(
        LoggedInContext loggedInContext,
        CreateProjectInput input,
        [Service] ProjectService projectService,
        LexBoxDbContext dbContext)
    {
        var projectId = await projectService.CreateProject(input, loggedInContext.User.Id);
        return await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    public async Task<Project> AddProjectMember(LoggedInContext loggedInContext, AddProjectMemberInput input, LexBoxDbContext dbContext)
    {
        loggedInContext.User.AssertCanManageProject(input.ProjectId);
        var user = await dbContext.Users.FirstOrDefaultAsync(u =>
            u.Username == input.UserEmail || u.Email == input.UserEmail);
        if (user is null) throw new NotFoundException("Member not found");
        dbContext.ProjectUsers.Add(
            new ProjectUsers { Role = input.Role, ProjectId = input.ProjectId, UserId = user.Id });
        await dbContext.SaveChangesAsync();
        return await dbContext.Projects.Include(p => p.Users).ThenInclude(u => u.User).Where(p => p.Id == input.ProjectId).FirstAsync();
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    public async Task<ProjectUsers> ChangeProjectMemberRole(
        ChangeProjectMemberRoleInput input,
        LoggedInContext loggedInContext,
        LexBoxDbContext dbContext)
    {
        loggedInContext.User.AssertCanManagerProjectMemberRole(input.ProjectId, input.UserId);
        var projectUser =
            await dbContext.ProjectUsers.FirstOrDefaultAsync(u =>
                u.ProjectId == input.ProjectId && u.UserId == input.UserId);
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
        LoggedInContext loggedInContext,
        LexBoxDbContext dbContext)
    {
        loggedInContext.User.AssertCanManageProject(input.ProjectId);
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
        LoggedInContext loggedInContext,
        LexBoxDbContext dbContext)
    {
        loggedInContext.User.AssertCanManageProject(input.ProjectId);
        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        if (project is null) throw new NotFoundException("Project not found");

        project.Description = input.Description;
        await dbContext.SaveChangesAsync();
        return project;
    }

    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IExecutable<Project>> RemoveProjectMember(RemoveProjectMemberInput input,
        LoggedInContext loggedInContext,
        LexBoxDbContext dbContext)
    {
        loggedInContext.User.AssertCanManageProject(input.ProjectId);
        await dbContext.ProjectUsers.Where(pu => pu.ProjectId == input.ProjectId && pu.UserId == input.UserId)
            .ExecuteDeleteAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId).AsExecutable();
    }


    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    public async Task<Project> SoftDeleteProject(
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

        return project;
    }
}
