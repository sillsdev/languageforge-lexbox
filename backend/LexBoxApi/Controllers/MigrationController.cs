using LexBoxApi.Auth;
using LexCore.Auth;
using LexCore.Entities;
using LexData;
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
    private readonly LexBoxDbContext _lexBoxDbContext;

    public MigrationController(RedmineDbContext redmineDbContext, LexBoxDbContext lexBoxDbContext)
    {
        _redmineDbContext = redmineDbContext;
        _lexBoxDbContext = lexBoxDbContext;
    }

    [HttpGet("dryRunTransformUser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<User>> DryRunTransformUser(string email)
    {

        var user = await _lexBoxDbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) return NotFound();
        return user;
    }

    [HttpGet("dryRunTransformProject")]
    public async Task<ActionResult<Project>> DryRunTransformProject(string code)
    {
        await MigrateData(false);
        var project = await _lexBoxDbContext.Projects.FirstOrDefaultAsync(p => p.Code == code);
        return project is null ? NotFound() : project;
    }

    [HttpGet("migrateData")]
    public async Task<ActionResult> MigrateData(bool prefixProjectCode = true, bool dryRun = true)
    {
        var now = DateTimeOffset.UtcNow;
        var projects = await _redmineDbContext.Projects.ToArrayAsync();
        var users = await _redmineDbContext.Users.Include(u => u.EmailAddresses).ToArrayAsync();
        await _redmineDbContext.Members.Include(p => p.Role).ToArrayAsync();
        await _redmineDbContext.Roles.ToArrayAsync();
        var projectIdToGuid = projects.ToDictionary(p => p.Id, p => Guid.NewGuid());
        _lexBoxDbContext.Projects.AddRange(projects.Select(rmProject => new Project
        {
            CreatedDate = rmProject.CreatedOn?.ToUniversalTime() ?? now,
            UpdatedDate = rmProject.UpdatedOn?.ToUniversalTime() ?? now,
            Name = rmProject.Name,
            Code = $"{rmProject.Identifier}{(prefixProjectCode ? "-lexbox" : "")}" ??
                   throw new Exception("no code for project id" + rmProject.Id),
            Description = rmProject.Description,
            Type = rmProject.Identifier!.EndsWith("-flex") ? ProjectType.FLEx : ProjectType.Unknown,
            RetentionPolicy =
                rmProject.Identifier.Contains("test") ? RetentionPolicy.Test : RetentionPolicy.Unknown,
            LastCommit = null,
            Id = projectIdToGuid[rmProject.Id],
            Users = new List<ProjectUsers>()
        }));

        _lexBoxDbContext.Users.AddRange(users.Select(rmUser => new User
        {
            CreatedDate = rmUser.CreatedOn?.ToUniversalTime() ?? now,
            UpdatedDate = rmUser.UpdatedOn?.ToUniversalTime() ?? now,
            Username = rmUser.Login,
            LocalizationCode = rmUser.Language ?? LexCore.Entities.User.DefaultLocalizationCode,
            Email = rmUser.EmailAddresses.FirstOrDefault()?.Address ?? "",
            Name = rmUser.Firstname + " " + rmUser.Lastname,
            IsAdmin = rmUser.Admin,
            Salt = rmUser.Salt ?? "",
            PasswordHash = rmUser.HashedPassword,
            EmailVerified = rmUser.Status != 2,
            Locked = rmUser.Status == 3,
            Projects = rmUser.ProjectMembership?.Select(m => new ProjectUsers
            {
                ProjectId = projectIdToGuid[m.ProjectId],
                Role = m.Role.Role.Name == "Manager" ? ProjectRole.Manager
                    : m.Role.Role.Name == "Contributor" ? ProjectRole.Editor : ProjectRole.Unknown,
                CreatedDate = m.CreatedOn?.ToUniversalTime() ?? now,
                UpdatedDate = m.CreatedOn?.ToUniversalTime() ?? now
            }).ToList() ?? new List<ProjectUsers>()
        }));
        if (!dryRun)
        {
            await using var transaction = await _lexBoxDbContext.Database.BeginTransactionAsync();
            await _lexBoxDbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        return Ok(_lexBoxDbContext.ChangeTracker.Entries().Count());
    }
}
