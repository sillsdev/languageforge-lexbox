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
using System.Net.Mail;

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

        if (!permissionService.HasProjectCreatePermission())
        {
            if (!permissionService.HasProjectRequestPermission()) throw new ProjectCreatorsMustHaveEmail("Project creators must have a valid email address");
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
    [Error<ProjectMembersMustBeVerifiedForRole>]
    [Error<ProjectMemberInvitedByEmail>]
    [Error<InvalidEmailException>]
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
        NotFoundException.ThrowIfNull(project);
        var user = await dbContext.Users.Include(u => u.Projects).FindByEmailOrUsername(input.UsernameOrEmail);
        if (user is null)
        {
            var (_, email, _) = ExtractNameAndAddressFromUsernameOrEmail(input.UsernameOrEmail);
            // We don't try to catch InvalidEmailException; if it happens, we let it get sent to the frontend
            if (email is null)
            {
                throw NotFoundException.ForType<User>();
            }
            else
            {
                var manager = loggedInContext.User;
                await emailService.SendCreateAccountEmail(email, input.ProjectId, input.Role, manager.Name, project.Name);
                throw new ProjectMemberInvitedByEmail("Invitation email sent");
            }
        }
        if (user.Projects.Any(p => p.ProjectId == input.ProjectId))
        {
            throw new AlreadyExistsException("User is already a member of this project");
        }

        user.AssertHasVerifiedEmailForRole(input.Role);
        user.UpdateCreateProjectsPermission(input.Role);
        dbContext.ProjectUsers.Add(
            new ProjectUsers { Role = input.Role, ProjectId = input.ProjectId, UserId = user.Id });
        user.UpdateUpdatedDate();
        project.UpdateUpdatedDate();
        await dbContext.SaveChangesAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId);
    }

    public record UserProjectRole(string Username, ProjectRole Role);
    public record BulkAddProjectMembersResult(List<UserProjectRole> AddedMembers, List<UserProjectRole> CreatedMembers, List<UserProjectRole> ExistingMembers);

    [Error<NotFoundException>]
    [Error<InvalidEmailException>]
    [Error<DbError>]
    [AdminRequired]
    [UseMutationConvention]
    public async Task<BulkAddProjectMembersResult> BulkAddProjectMembers(
        LoggedInContext loggedInContext,
        BulkAddProjectMembersInput input,
        LexBoxDbContext dbContext)
    {
        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        if (project is null) throw new NotFoundException("Project not found", "project");
        List<UserProjectRole> AddedMembers = [];
        List<UserProjectRole> CreatedMembers = [];
        List<UserProjectRole> ExistingMembers = [];
        var existingUsers = await dbContext.Users.Include(u => u.Projects).Where(u => input.Usernames.Contains(u.Username) || input.Usernames.Contains(u.Email)).ToArrayAsync();
        var byUsername = existingUsers.Where(u => u.Username is not null).ToDictionary(u => u.Username!);
        var byEmail = existingUsers.Where(u => u.Email is not null).ToDictionary(u => u.Email!);
        foreach (var usernameOrEmail in input.Usernames)
        {
            var user = byUsername.GetValueOrDefault(usernameOrEmail) ?? byEmail.GetValueOrDefault(usernameOrEmail);
            if (user is null)
            {
                var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(SHA1.HashSizeInBytes));
                var (name, email, username) = ExtractNameAndAddressFromUsernameOrEmail(usernameOrEmail);
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    Name = name,
                    Email = email,
                    LocalizationCode = "en", // TODO: input.Locale,
                    Salt = salt,
                    PasswordHash = PasswordHashing.HashPassword(input.PasswordHash, salt, true),
                    PasswordStrength = 0, // Shared password, so always considered strength 0, we don't call Zxcvbn here
                    IsAdmin = false,
                    EmailVerified = false,
                    CreatedById = loggedInContext.User.Id,
                    Locked = false,
                    CanCreateProjects = false
                };
                CreatedMembers.Add(new UserProjectRole(usernameOrEmail, input.Role));
                user.Projects.Add(new ProjectUsers { Role = input.Role, ProjectId = input.ProjectId, UserId = user.Id });
                dbContext.Add(user);
            }
            else
            {
                var userProject = user.Projects.FirstOrDefault(p => p.ProjectId == input.ProjectId);
                if (userProject is not null)
                {
                    ExistingMembers.Add(new UserProjectRole(user.Username ?? user.Email!, userProject.Role));
                }
                else
                {
                    AddedMembers.Add(new UserProjectRole(user.Username ?? user.Email!, input.Role));
                    // Not yet a member, so add a membership. We don't want to touch existing memberships, which might have other roles
                    user.Projects.Add(new ProjectUsers { Role = input.Role, ProjectId = input.ProjectId, UserId = user.Id });
                }
            }
        }
        await dbContext.SaveChangesAsync();
        return new BulkAddProjectMembersResult(AddedMembers, CreatedMembers, ExistingMembers);
    }

    public static (string name, string? email, string? username) ExtractNameAndAddressFromUsernameOrEmail(string usernameOrEmail)
    {
        var isEmailAddress = usernameOrEmail.Contains('@');
        string name;
        string? email;
        string? username;
        if (isEmailAddress)
        {
            try
            {
                var parsed = new MailAddress(usernameOrEmail);
                email = parsed.Address;
                username = null;
                name = parsed.DisplayName;
                if (string.IsNullOrEmpty(name)) name = email.Split('@')[0];
            }
            catch (FormatException)
            {
                // FormatException message from .NET talks about mail headers, which is confusing here
                throw new InvalidEmailException("Invalid email address", usernameOrEmail);
            }
        }
        else
        {
            username = usernameOrEmail;
            email = null;
            name = username;
        }
        return (name, email, username);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<ProjectMembersMustBeVerified>]
    [Error<ProjectMembersMustBeVerifiedForRole>]
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
            await dbContext.ProjectUsers
                .Include(r => r.Project)
                .Include(r => r.User)
                .FirstOrDefaultAsync(u => u.ProjectId == input.ProjectId && u.UserId == input.UserId);
        if (projectUser?.User is null || projectUser.Project is null) throw NotFoundException.ForType<ProjectUsers>();
        projectUser.User.AssertHasVerifiedEmailForRole(input.Role);
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
        NotFoundException.ThrowIfNull(project);

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
        NotFoundException.ThrowIfNull(project);

        project.Description = input.Description;
        project.UpdateUpdatedDate();
        await dbContext.SaveChangesAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> SetProjectConfidentiality(SetProjectConfidentialityInput input,
        IPermissionService permissionService,
        LexBoxDbContext dbContext)
    {
        permissionService.AssertCanManageProject(input.ProjectId);
        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        NotFoundException.ThrowIfNull(project);

        project.IsConfidential = input.IsConfidential;
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
        NotFoundException.ThrowIfNull(project);
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
    [AdminRequired]
    [UseMutationConvention]
    [UseProjection]
    public async Task<DraftProject> DeleteDraftProject(
        Guid draftProjectId,
        LexBoxDbContext dbContext)
    {
        var deletedDraft = await dbContext.DraftProjects.FindAsync(draftProjectId);
        if (deletedDraft == null)
        {
            throw NotFoundException.ForType<DraftProject>();
        }
        // Draft projects are deleted immediately, not soft-deleted
        dbContext.DraftProjects.Remove(deletedDraft);
        await dbContext.SaveChangesAsync();
        return deletedDraft;
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
        if (project is null)
        {
            // Draft projects, if any, are deleted immediately, not soft-deleted
            var deletedDraftCount = await dbContext.DraftProjects.Where(dp => dp.Id == projectId).ExecuteDeleteAsync();
            if (deletedDraftCount == 0)
            {
                // No draft project either, so return standard project not found error
                throw NotFoundException.ForType<Project>();
            }
            else
            {
                // Return an empty project list to indicate success
                return dbContext.Projects.Where(p => p.Id == projectId);
            }
        }
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
