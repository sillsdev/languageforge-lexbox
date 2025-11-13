using LexBoxApi.Auth;
using System.Security.Cryptography;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.GraphQL.CustomTypes;
using LexBoxApi.Models.Project;
using LexBoxApi.Services;
using LexBoxApi.Services.Email;
using LexCore;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexCore.Utils;
using LexData;
using LexData.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace LexBoxApi.GraphQL;

[MutationType]
public class ProjectMutations
{
    public enum CreateProjectResult
    {
        Created,
        Requested
    }

    public record CreateProjectResponse(Guid Id, CreateProjectResult Result);
    [Error<DbError>]
    [Error<AlreadyExistsException>]
    [Error<ProjectCreatorsMustHaveEmail>]
    [UseMutationConvention]
    [RefreshJwt]
    [VerifiedEmailRequired]
    public async Task<CreateProjectResponse> CreateProject(
        LoggedInContext loggedInContext,
        IPermissionService permissionService,
        CreateProjectInput input,
        ProjectService projectService,
        IEmailService emailService)
    {
        if (!loggedInContext.User.IsAdmin || input.ForceDraft) //draft projects should always have a manager
        {
            // For non-admins we always implicitly set them as the project manager
            // Only admins can create empty projects or projects for other users
            input = input with { ProjectManagerId = loggedInContext.User.Id };
        }

        if (!permissionService.HasProjectCreatePermission() || input.ForceDraft)
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
    public async Task<IQueryable<Project>> AddProjectMember(
        IPermissionService permissionService,
        LoggedInContext loggedInContext,
        AddProjectMemberInput input,
        LexBoxDbContext dbContext,
        IEmailService emailService)
    {
        await permissionService.AssertCanManageProject(input.ProjectId);
        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        NotFoundException.ThrowIfNull(project);
        User? user;
        if (input.UserId is not null)
        {
            user = await dbContext.Users.Include(u => u.Projects).Where(u => u.Id == input.UserId).FirstOrDefaultAsync();
            NotFoundException.ThrowIfNull(user);
        }
        else
        {
            user = await dbContext.Users.Include(u => u.Projects).FindByEmailOrUsername(input.UsernameOrEmail ?? "");
            if (user is null)
            {
                var (_, email, _) = UserService.ExtractNameAndAddressFromUsernameOrEmail(input.UsernameOrEmail ?? "");
                // We don't try to catch InvalidEmailException; if it happens, we let it get sent to the frontend
                if (email is null)
                {
                    throw NotFoundException.ForType<User>();
                }
                else if (input.canInvite)
                {
                    var manager = loggedInContext.User;
                    await emailService.SendCreateAccountWithProjectEmail(
                        email,
                        manager.Name,
                        projectId: input.ProjectId,
                        role: input.Role,
                        projectName: project.Name);
                    throw new ProjectMemberInvitedByEmail("Invitation email sent");
                }
                else
                {
                    throw NotFoundException.ForType<User>();
                }
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
        await emailService.SendUserAddedEmail(user, project.Name, project.Code);
        return dbContext.Projects.Where(p => p.Id == input.ProjectId);
    }

    public record UserProjectRole(string Username, ProjectRole Role);
    public record BulkAddProjectMembersResult(List<UserProjectRole> AddedMembers, List<UserProjectRole> CreatedMembers, List<UserProjectRole> ExistingMembers);

    [Error<NotFoundException>]
    [Error<InvalidEmailException>]
    [Error<DbError>]
    [UseMutationConvention]
    public async Task<BulkAddProjectMembersResult> BulkAddProjectMembers(
        IPermissionService permissionService,
        LoggedInContext loggedInContext,
        BulkAddProjectMembersInput input,
        LexBoxDbContext dbContext)
    {
        await permissionService.AssertCanCreateGuestUserInProject(input.ProjectId);
        var projectExists = await dbContext.Projects.AnyAsync(p => p.Id == input.ProjectId);
        if (!projectExists) throw new NotFoundException("Project not found", "project");
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
                var (name, email, username) = UserService.ExtractNameAndAddressFromUsernameOrEmail(usernameOrEmail);
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
        await permissionService.AssertCanManageProjectMemberRole(input.ProjectId, input.UserId);
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
    [Error<ProjectMembersMustBeVerified>]
    [Error<ProjectMembersMustBeVerifiedForRole>]
    [Error<ProjectHasNoManagers>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> AskToJoinProject(
        IPermissionService permissionService,
        LoggedInContext loggedInContext,
        Guid projectId,
        LexBoxDbContext dbContext,
        IEmailService emailService)
    {
        await permissionService.AssertCanAskToJoinProject(projectId);

        var user = await dbContext.Users.FindAsync(loggedInContext.User.Id);
        if (user is null) throw new UnauthorizedAccessException();
        user.AssertHasVerifiedEmailForRole(ProjectRole.Editor);

        var project = await dbContext.Projects
            .Include(p => p.Users)
            .ThenInclude(u => u.User)
            .Where(p => p.Id == projectId)
            .FirstOrDefaultAsync();
        NotFoundException.ThrowIfNull(project);

        var managers = project.Users.Where(u => u.Role == ProjectRole.Manager);
        var emailsSent = 0;
        foreach (var manager in managers)
        {
            if (manager.User is null) continue;
            await emailService.SendJoinProjectRequestEmail(manager.User, user, project);
            emailsSent++;
        }
        if (emailsSent == 0) throw new ProjectHasNoManagers(project.Code);
        return dbContext.Projects.Where(p => p.Id == projectId);
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
        await permissionService.AssertCanManageProject(input.ProjectId);
        if (string.IsNullOrEmpty(input.Name)) throw new RequiredException("Project name cannot be empty");

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
        await permissionService.AssertCanManageProject(input.ProjectId);
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
        ProjectService projectService,
        LexBoxDbContext dbContext)
    {
        await permissionService.AssertCanManageProject(input.ProjectId);
        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        NotFoundException.ThrowIfNull(project);

        project.IsConfidential = input.IsConfidential;
        project.UpdateUpdatedDate();
        projectService.InvalidateProjectConfidentialityCache(input.ProjectId);
        await dbContext.SaveChangesAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> SetRetentionPolicy(
        SetRetentionPolicyInput input,
        IPermissionService permissionService,
        ProjectService projectService,
        LexBoxDbContext dbContext)
    {
        await permissionService.AssertCanManageProject(input.ProjectId);
        var project = await dbContext.Projects.FindAsync(input.ProjectId);
        NotFoundException.ThrowIfNull(project);

        project.RetentionPolicy = input.RetentionPolicy;
        project.UpdateUpdatedDate();
        await dbContext.SaveChangesAsync();
        return dbContext.Projects.Where(p => p.Id == input.ProjectId);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<UnauthorizedAccessException>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> UpdateProjectRepoSizeInKb(string code,
        IPermissionService permissionService,
        ProjectService projectService,
        LexBoxDbContext dbContext)
    {
        var projectId = await projectService.LookupProjectId(code);
        if (projectId is null) throw NotFoundException.ForType<Project>();
        await permissionService.AssertCanViewProject(projectId.Value);
        await projectService.UpdateRepoSizeInKb(code);
        return dbContext.Projects.Where(p => p.Id == projectId);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<UnauthorizedAccessException>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> UpdateProjectLexEntryCount(string code,
        IPermissionService permissionService,
        ProjectService projectService,
        LexBoxDbContext dbContext)
    {
        var projectId = await projectService.LookupProjectId(code);
        if (projectId is null) throw NotFoundException.ForType<Project>();
        await permissionService.AssertCanManageProject(projectId.Value);
        var project = await dbContext.Projects.FindAsync(projectId);
        NotFoundException.ThrowIfNull(project);
        var result = await projectService.UpdateLexEntryCount(code);
        return dbContext.Projects.Where(p => p.Id == projectId);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<UnauthorizedAccessException>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> UpdateProjectLanguageList(string code,
        IPermissionService permissionService,
        ProjectService projectService,
        LexBoxDbContext dbContext)
    {
        var projectId = await projectService.LookupProjectId(code);
        if (projectId is null) throw NotFoundException.ForType<Project>();
        await permissionService.AssertCanManageProject(projectId.Value);
        var project = await dbContext.Projects.FindAsync(projectId);
        NotFoundException.ThrowIfNull(project);
        await projectService.UpdateProjectLangTags(projectId.Value);
        return dbContext.Projects.Where(p => p.Id == projectId);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<UnauthorizedAccessException>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> UpdateLangProjectId(string code,
        IPermissionService permissionService,
        ProjectService projectService,
        LexBoxDbContext dbContext)
    {
        var projectId = await projectService.LookupProjectId(code);
        if (projectId is null) throw NotFoundException.ForType<Project>();
        await permissionService.AssertCanManageProject(projectId.Value);
        var project = await dbContext.Projects.FindAsync(projectId);
        NotFoundException.ThrowIfNull(project);
        await projectService.UpdateProjectLangProjectId(projectId.Value);
        return dbContext.Projects.Where(p => p.Id == projectId);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<UnauthorizedAccessException>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> UpdateFLExModelVersion(string code,
        IPermissionService permissionService,
        ProjectService projectService,
        LexBoxDbContext dbContext)
    {
        var projectId = await projectService.LookupProjectId(code);
        if (projectId is null) throw NotFoundException.ForType<Project>();
        await permissionService.AssertCanManageProject(projectId.Value);
        await projectService.UpdateFLExModelVersion(projectId.Value);
        return dbContext.Projects.Where(p => p.Id == projectId);
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
        await permissionService.AssertCanManageProject(input.ProjectId);
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
    [Error<ProjectSyncInProgressException>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> SoftDeleteProject(
        Guid projectId,
        IPermissionService permissionService,
        ProjectService projectService,
        LexBoxDbContext dbContext,
        IHgService hgService,
        ILogger<ProjectMutations> _logger,
        FwHeadlessClient fwHeadlessClient)
    {
        await permissionService.AssertCanManageProject(projectId);

        var project = await dbContext.Projects.Include(p => p.Users).FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null)
        {
            throw NotFoundException.ForType<Project>();
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
        await fwHeadlessClient.DeleteRepo(project.Id);
        try
        {
            await hgService.SoftDeleteRepo(projectCode, timestamp);
        }
        catch (DirectoryNotFoundException e)
        {
            // If the repo doesn't exist, we don't need to delete it
            Activity.Current?.AddTag("app.hg.delete", "not-found");
            _logger.LogWarning(e, "Failed to soft-delete repo while soft-deleting project {ProjectCode}", projectCode);
        }

        projectService.InvalidateProjectConfidentialityCache(projectId);
        projectService.InvalidateProjectCodeCache(projectCode);
        await transaction.CommitAsync();

        return dbContext.Projects.Where(p => p.Id == projectId);
    }
}
