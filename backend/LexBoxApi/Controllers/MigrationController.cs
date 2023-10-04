using LexBoxApi.Auth;
using LexBoxApi.Services;
using LexCore.Auth;
using LexCore.Entities;
using LexData;
using LexData.Entities;
using LexData.Redmine;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/migrate")]
[AdminRequired]
public class MigrationController : ControllerBase
{
    private readonly RedmineDbContext _redminePublicDbContext;
    private readonly RedmineDbContext _privateRedmineDbContext;
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly ProjectService _projectService;

    public MigrationController(PublicRedmineDbContext redmineDbContext,
        PrivateRedmineDbContext privateRedmineDbContext,
        LexBoxDbContext lexBoxDbContext,
        ProjectService projectService)
    {
        _redminePublicDbContext = redmineDbContext;
        _lexBoxDbContext = lexBoxDbContext;
        _projectService = projectService;
        _privateRedmineDbContext = privateRedmineDbContext;
    }

    [HttpGet("dryRunTransformUser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<User>> DryRunTransformUser(string email)
    {
        var user = await _lexBoxDbContext.Users.FindByEmail(email);
        if (user is null) return NotFound();
        return user;
    }

    [HttpGet("dryRunTransformProject")]
    public async Task<ActionResult<object>> DryRunTransformProject(string code)
    {
        await using var transaction = await _lexBoxDbContext.Database.BeginTransactionAsync();
        await MigrateData(false);
        var project = await _lexBoxDbContext.Projects.FirstOrDefaultAsync(p => p.Code == code);
        await transaction.RollbackAsync();
        return project is null
            ? NotFound()
            : new
            {
                Name = project.Name, Code = project.Code, Description = project.Description, Type = project.Type,
            };
    }

    [HttpGet("migrateData")]
    public async Task<ActionResult> MigrateData(bool dryRun = true)
    {
        var now = DateTimeOffset.UtcNow;
        await using var transaction = await _lexBoxDbContext.Database.BeginTransactionAsync();
        await Migrate(_redminePublicDbContext, now);
        if (!dryRun) await _lexBoxDbContext.SaveChangesAsync();
        await Migrate(_privateRedmineDbContext, now);
        if (!dryRun)
        {
            await _lexBoxDbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        return Ok(_lexBoxDbContext.ChangeTracker.Entries().Count());
    }

    private async Task Migrate(RedmineDbContext dbContext, DateTimeOffset now)
    {
        var existingUsers =
            await _lexBoxDbContext.Users.ToDictionaryAsync(u => u.Email, u => u, StringComparer.OrdinalIgnoreCase);
        var projects = await dbContext.Projects.ToArrayAsync();
        //filter out empty login because there's some default redmine accounts without a login
        var users = await dbContext.Users.Where(u => u.Login != "").Include(u => u.EmailAddresses)
            .ToArrayAsync();
        await dbContext.Members.Include(p => p.Role).ToArrayAsync();
        await dbContext.Roles.ToArrayAsync();
        //todo set based on redmine db
        var migrationStatus = dbContext is PublicRedmineDbContext
            ? ProjectMigrationStatus.PublicRedmine
            : ProjectMigrationStatus.PrivateRedmine;
        var projectIdToGuid = projects.ToDictionary(p => p.Id, p => Guid.NewGuid());
        _lexBoxDbContext.Projects.AddRange(projects.Select(rmProject =>
            MigrateProject(rmProject, now, migrationStatus, projectIdToGuid)));
        _lexBoxDbContext.Users.AddRange(users.Select(rmUser => MigrateUser(rmUser, projectIdToGuid, now, existingUsers))
            .OfType<User>());
    }

    private static User? MigrateUser(RmUser rmUser,
        Dictionary<int, Guid> projectIdToGuid,
        DateTimeOffset now,
        Dictionary<string, User> existingUsers)
    {
        var userProjects = rmUser.ProjectMembership?.Where(m => m.Project is not null).Select(m => new ProjectUsers
        {
            ProjectId = projectIdToGuid[m.ProjectId],
            Role = m.Role.Role.Name switch
            {
                "Manager" => ProjectRole.Manager,
                "Contributor" => ProjectRole.Editor,
                _ => ProjectRole.Unknown
            },
            CreatedDate = m.CreatedOn?.ToUniversalTime() ?? now,
            UpdatedDate = m.CreatedOn?.ToUniversalTime() ?? now
        }).ToList() ?? new List<ProjectUsers>();
        var email = rmUser.EmailAddresses.FirstOrDefault()?.Address;
        if (email is null) throw new Exception("no email for user id: " + rmUser.Login);
        if (existingUsers.TryGetValue(email, out var user))
        {
            //modify existing user to merge users from public and private
            user.Projects.AddRange(userProjects);
            if (rmUser.Admin)
            {
                user.IsAdmin = true;
            }

            if (!user.CanCreateProjects && userProjects.Any(p => p.Role == ProjectRole.Manager))
            {
                user.CanCreateProjects = true;
            }

            //a new user wasnt added
            return null;
        }

        return new User
        {
            CreatedDate = rmUser.CreatedOn?.ToUniversalTime() ?? now,
            UpdatedDate = rmUser.UpdatedOn?.ToUniversalTime() ?? now,
            LastActive = rmUser.LastLoginOn?.ToUniversalTime() ?? now,
            Username = rmUser.Login,
            LocalizationCode = rmUser.Language ?? LexCore.Entities.User.DefaultLocalizationCode,
            Email = email,
            Name = rmUser.Firstname + " " + rmUser.Lastname,
            IsAdmin = rmUser.Admin,
            Salt = rmUser.Salt ?? "",
            PasswordHash = rmUser.HashedPassword,
            EmailVerified = rmUser.Status != 2,
            Locked = rmUser.Status == 3,
            Projects = userProjects,
            CanCreateProjects = userProjects.Any(p => p.Role == ProjectRole.Manager)
        };
    }

    private static Project MigrateProject(RmProject rmProject,
        DateTimeOffset now,
        ProjectMigrationStatus migrationStatus,
        Dictionary<int, Guid> projectIdToGuid)
    {
        return new Project
        {
            CreatedDate = rmProject.CreatedOn?.ToUniversalTime() ?? now,
            UpdatedDate = rmProject.UpdatedOn?.ToUniversalTime() ?? now,
            MigrationStatus = migrationStatus,
            ProjectOrigin = migrationStatus,
            Name = rmProject.Name,
            Code = rmProject.Identifier ?? throw new Exception("no code for project id" + rmProject.Id),
            Description = rmProject.Description,
            Type = rmProject.Identifier!.EndsWith("-flex") ? ProjectType.FLEx : ProjectType.Unknown,
            RetentionPolicy =
                rmProject.Identifier.Contains("test") ? RetentionPolicy.Test : RetentionPolicy.Unknown,
            LastCommit = null,
            Id = projectIdToGuid[rmProject.Id],
            ParentId = rmProject.ParentId switch
            {
                null => null,
                _ => projectIdToGuid[rmProject.ParentId.Value]
            },
            Users = new List<ProjectUsers>()
        };
    }

    [HttpGet("migrateRepo")]
    public async Task<ActionResult> MigrateRepo(string projectCode)
    {
        await _projectService.MigrateProject(projectCode);
        return Ok();
    }

    [HttpGet("migrateStatus")]
    public async Task<ActionResult<ProjectMigrationStatus>> MigrateStatus(string projectCode)
    {
        return await _lexBoxDbContext.Projects
            .Where(p => p.Code == projectCode)
            .Select(p => p.MigrationStatus)
            .SingleAsync();
    }

    [HttpGet("awaitMigrated")]
    public async Task<ActionResult<bool>> AwaitMigrated(string projectCode)
    {
        var token = CancellationTokenSource.CreateLinkedTokenSource(
            new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token,
            HttpContext.RequestAborted
        ).Token;
        return await _projectService.AwaitMigration(projectCode, token);
    }
}
