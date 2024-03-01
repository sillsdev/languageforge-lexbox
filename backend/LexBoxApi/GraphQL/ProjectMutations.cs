using LexBoxApi.Auth;
using System.Security.Cryptography;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.GraphQL.CustomTypes;
using LexBoxApi.Models.Project;
using LexBoxApi.Services;
using LexCore;
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
    [Error<ProjectCreatorsMustHaveEmail>]
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

        if (loggedInContext.User.Email == null) throw new ProjectCreatorsMustHaveEmail("Project creators must have an email address");

        if (!permissionService.HasProjectCreatePermission())
        {
            await emailService.SendCreateProjectRequestEmail(loggedInContext.User, input);
            return new CreateProjectResponse(null, CreateProjectResult.Requested);
        }

        var projectId = await projectService.CreateProject(input);
        return new CreateProjectResponse(projectId, CreateProjectResult.Created);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<ProjectMembersMustBeVerified>]
    [Error<ProjectMemberInvitedByEmail>]
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
        var user = await dbContext.Users.FindByEmail(input.UserEmail);
        if (user is null)
        {
            var manager = loggedInContext.User;
            await emailService.SendCreateAccountEmail(input.UserEmail, input.ProjectId, input.Role, manager.Name, project.Name);
            throw new ProjectMemberInvitedByEmail("Invitation email sent");
        }
        if (!user.EmailVerified) throw new ProjectMembersMustBeVerified("Member must verify email first");
        user.UpdateCreateProjectsPermission(input.Role);
        dbContext.ProjectUsers.Add(
            new ProjectUsers { Role = input.Role, ProjectId = input.ProjectId, UserId = user.Id });
        await dbContext.SaveChangesAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<ProjectMembersMustBeVerified>]
    [Error<ProjectMemberInvitedByEmail>]
    [AdminRequired]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<BulkAddProjectMembersResult> BulkAddProjectMembers(
        LoggedInContext loggedInContext,
        BulkAddProjectMembersInput input,
        LexBoxDbContext dbContext)
    {
        var admin = await dbContext.Users.FindAsync(loggedInContext.User.Id);
        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        // if (project is null) return NotFound();
        List<string> usernameConflicts = [];
        int count = 0;
        foreach (var username in input.Usernames)
        {
            var user = await dbContext.Users.Where(u => u.Username == username).FirstOrDefaultAsync();
            if (user is null)
            {
                count++;
                var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(SHA1.HashSizeInBytes));
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    Name = username,
                    Email = "",
                    LocalizationCode = "en", // TODO: input.Locale,
                    Salt = salt,
                    PasswordHash = PasswordHashing.HashPassword(input.PasswordHash, salt, true),
                    IsAdmin = false,
                    EmailVerified = false,
                    CreatedBy = admin,
                    Locked = false,
                    CanCreateProjects = false
                };
                user.Projects.Add(new ProjectUsers { Role = input.Role, ProjectId = input.ProjectId, UserId = user.Id });
                dbContext.Add(user);
            }
            else
            {
                usernameConflicts.Add(username);
                var projectUser = await dbContext.ProjectUsers.FirstOrDefaultAsync(
                    u => u.ProjectId == input.ProjectId && u.UserId == user.Id);
                if (projectUser is null)
                {
                    user.Projects.Add(new ProjectUsers { Role = input.Role, ProjectId = input.ProjectId, UserId = user.Id });
                }
            }
        }
        await dbContext.SaveChangesAsync();
        return new BulkAddProjectMembersResult(count, usernameConflicts);
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
        IPermissionService permissionService,
        LexBoxDbContext dbContext)
    {
        permissionService.AssertCanManageProject(input.ProjectId);
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
        IPermissionService permissionService,
        LexBoxDbContext dbContext)
    {
        permissionService.AssertCanManageProject(input.ProjectId);
        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        if (project is null) throw new NotFoundException("Project not found");

        project.Description = input.Description;
        await dbContext.SaveChangesAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId);
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
