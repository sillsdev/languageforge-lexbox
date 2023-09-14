using LexBoxApi.Auth;
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
    private readonly RedmineDbContext _redmineDbContext;
    private readonly RedmineDbContext _privateRedmineDbContext;
    private readonly LexBoxDbContext _lexBoxDbContext;

    public MigrationController(PublicRedmineDbContext redmineDbContext, PrivateRedmineDbContext privateRedmineDbContext, LexBoxDbContext lexBoxDbContext)
    {
        _redmineDbContext = redmineDbContext;
        _lexBoxDbContext = lexBoxDbContext;
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
        return project is null ? NotFound() : new
        {
            Name = project.Name,
            Code = project.Code,
            Description = project.Description,
            Type = project.Type,
        };
    }

    [HttpGet("migrateData")]
    public async Task<ActionResult> MigrateData(bool dryRun = true)
    {
        //todo use private redmine db to migrate data also
        var now = DateTimeOffset.UtcNow;
        var projects = await _redmineDbContext.Projects.ToArrayAsync();
        //filter out empty login because there's some default redmine accounts without a login
        var users = await _redmineDbContext.Users.Where(u => u.Login != "").Include(u => u.EmailAddresses).ToArrayAsync();
        await _redmineDbContext.Members.Include(p => p.Role).ToArrayAsync();
        await _redmineDbContext.Roles.ToArrayAsync();
        //todo set based on redmine db
        var migrationStatus = ProjectMigrationStatus.PublicRedmine;
        var projectIdToGuid = projects.ToDictionary(p => p.Id, p => Guid.NewGuid());
        _lexBoxDbContext.Projects.AddRange(projects.Select(rmProject => MigrateProject(rmProject, now, migrationStatus, projectIdToGuid)));
        _lexBoxDbContext.Users.AddRange(users.Select(rmUser => MigrateUser(rmUser, projectIdToGuid, now)));
        if (!dryRun)
        {
            await _lexBoxDbContext.SaveChangesAsync();
        }

        return Ok(_lexBoxDbContext.ChangeTracker.Entries().Count());
    }

    private static User MigrateUser(RmUser rmUser, Dictionary<int, Guid> projectIdToGuid, DateTimeOffset now)
    {
        var userProjects = rmUser.ProjectMembership?.Select(m => new ProjectUsers
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
        return new User
        {
            CreatedDate = rmUser.CreatedOn?.ToUniversalTime() ?? now,
            UpdatedDate = rmUser.UpdatedOn?.ToUniversalTime() ?? now,
            Username = rmUser.Login,
            LocalizationCode = rmUser.Language ?? LexCore.Entities.User.DefaultLocalizationCode,
            Email =
                rmUser.EmailAddresses.FirstOrDefault()?.Address ??
                throw new Exception("no email for user id" + rmUser.Login),
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
}
