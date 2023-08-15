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
        var user = await _redmineDbContext.Users
            .Where(user => user.EmailAddresses.Any(e => e.Address == email))
            .Select(rmUser => new User
            {
                CreatedDate = rmUser.CreatedOn ?? DateTime.UtcNow,
                UpdatedDate = rmUser.UpdatedOn ?? DateTime.UtcNow,
                Username = rmUser.Login,
                Email = email,
                Name = rmUser.Firstname + " " + rmUser.Lastname,
                IsAdmin = rmUser.Admin,
                Salt = rmUser.Salt ?? "",
                PasswordHash = rmUser.HashedPassword,
                EmailVerified = rmUser.Status != 2,
                Locked = rmUser.Status == 3,
                Projects = rmUser.ProjectMembership.Select(m => new ProjectUsers
                {
                    Role = m.Role.Role.Name == "Manager" ? ProjectRole.Manager
                        : m.Role.Role.Name == "Contributor" ? ProjectRole.Editor : ProjectRole.Unknown,
                    CreatedDate = m.CreatedOn ?? DateTime.UtcNow,
                    UpdatedDate = m.CreatedOn ?? DateTime.UtcNow,
                }).ToList()
            }).FirstOrDefaultAsync();

        if (user is null) return NotFound();
        return user;
    }

    [HttpGet("dryRunTransformProject")]
    public async Task<ActionResult<Project>> DryRunTransformProject(string code)
    {
        var project = await _redmineDbContext.Projects
            .Where(p => p.Identifier == code)
            .Select(rmProject => new Project
            {
                CreatedDate = rmProject.CreatedOn ?? DateTime.UtcNow,
                UpdatedDate = rmProject.UpdatedOn ?? DateTime.UtcNow,
                Name = rmProject.Name,
                Code = code,
                Description = rmProject.Description,
                Users = rmProject.Members.Select(m => new ProjectUsers
                {
                    Role = m.Role.Role.Name == "Manager" ? ProjectRole.Manager
                        : m.Role.Role.Name == "Contributor" ? ProjectRole.Editor : ProjectRole.Unknown,
                    CreatedDate = m.CreatedOn ?? DateTime.UtcNow,
                    UpdatedDate = m.CreatedOn ?? DateTime.UtcNow,
                    User = new User
                    {
                        CreatedDate = m.User.CreatedOn ?? DateTime.UtcNow,
                        UpdatedDate = m.User.UpdatedOn ?? DateTime.UtcNow,
                        Username = m.User.Login,
                        Email = m.User.EmailAddresses.FirstOrDefault()!.Address ?? "",
                        Name = m.User.Firstname + " " + m.User.Lastname,
                        IsAdmin = m.User.Admin,
                        Salt = m.User.Salt ?? "",
                        PasswordHash = m.User.HashedPassword,
                        EmailVerified = m.User.Status != 2,
                        Locked = m.User.Status == 3,
                    }
                }).ToList(),
                Type = rmProject.Identifier!.EndsWith("-flex") ? ProjectType.FLEx : ProjectType.Unknown,
                RetentionPolicy =
                    rmProject.Identifier.Contains("test") ? RetentionPolicy.Test : RetentionPolicy.Unknown,
                LastCommit = null
            })
            .FirstOrDefaultAsync();
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
