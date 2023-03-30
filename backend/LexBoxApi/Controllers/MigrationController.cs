using LexCore.Auth;
using LexCore.Entities;
using LexData.Redmine;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/migrate")]
public class MigrationController : ControllerBase
{
    private readonly RedmineDbContext _redmineDbContext;

    public MigrationController(RedmineDbContext redmineDbContext)
    {
        _redmineDbContext = redmineDbContext;
    }

    [HttpGet("dryRunTransformUser")]
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
                        Email = m.User.EmailAddresses.FirstOrDefault().Address ?? "",
                        Name = m.User.Firstname + " " + m.User.Lastname,
                        IsAdmin = m.User.Admin,
                        Salt = m.User.Salt ?? "",
                        PasswordHash = m.User.HashedPassword,
                    }
                }).ToList(),
                Type = rmProject.Identifier.EndsWith("-flex") ? ProjectType.FLEx: ProjectType.Unknown,
                RetentionPolicy = rmProject.Identifier.Contains("test") ? RetentionPolicy.Test : RetentionPolicy.Unknown,
                LastCommit = null
            })
            .FirstOrDefaultAsync();
        return project is null ? NotFound() : project;
    }
}