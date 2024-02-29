using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Services;
using LexCore;
using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/admin")]
public class AdminController : ControllerBase
{
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly LoggedInContext _loggedInContext;
    private readonly EmailService _emailService;

    public AdminController(LexBoxDbContext lexBoxDbContext,
        LoggedInContext loggedInContext,
        EmailService emailService
    )
    {
        _lexBoxDbContext = lexBoxDbContext;
        _loggedInContext = loggedInContext;
        _emailService = emailService;
    }

    public record ResetPasswordAdminRequest([Required(AllowEmptyStrings = false)] string PasswordHash, Guid userId);

    [HttpPost("resetPassword")]
    [AdminRequired]
    public async Task<ActionResult> ResetPasswordAdmin(ResetPasswordAdminRequest request)
    {
        var passwordHash = request.PasswordHash;
        var user = await _lexBoxDbContext.Users.FirstAsync(u => u.Id == request.userId);
        user.PasswordHash = PasswordHashing.HashPassword(passwordHash, user.Salt, true);
        user.UpdateUpdatedDate();
        await _lexBoxDbContext.SaveChangesAsync();
        await _emailService.SendPasswordChangedEmail(user);
        return Ok();
    }

    public record BulkCreateUsersAdminRequest([Required(AllowEmptyStrings = false)] string PasswordHash, List<string> Usernames, Guid ProjectId);

    [HttpPost("bulkCreateUsers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AdminRequired]
    public async Task<ActionResult> BulkCreateUsersAdmin(BulkCreateUsersAdminRequest request)
    {
        var admin = await _lexBoxDbContext.Users.FindAsync(_loggedInContext.User.Id);
        var project = await _lexBoxDbContext.Projects.FindAsync(request.ProjectId);
        if (project is null) return NotFound();
        List<string> usernameConflicts = [];
        int count = 0;
        foreach (var username in request.Usernames)
        {
            var user = await _lexBoxDbContext.Users.Where(u => u.Username == username).FirstOrDefaultAsync();
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
                    LocalizationCode = "en", // TODO: request.Locale,
                    Salt = salt,
                    PasswordHash = PasswordHashing.HashPassword(request.PasswordHash, salt, true),
                    IsAdmin = false,
                    EmailVerified = false,
                    CreatedBy = admin,
                    Locked = false,
                    CanCreateProjects = false
                };
                user.Projects.Add(new ProjectUsers { Role = ProjectRole.Editor, ProjectId = request.ProjectId, UserId = user.Id });
                _lexBoxDbContext.Add(user);
            }
            else
            {
                usernameConflicts.Add(username);
                var projectUser = await _lexBoxDbContext.ProjectUsers.FirstOrDefaultAsync(
                    u => u.ProjectId == request.ProjectId && u.UserId == user.Id);
                if (projectUser is null)
                {
                    user.Projects.Add(new ProjectUsers { Role = ProjectRole.Editor, ProjectId = request.ProjectId, UserId = user.Id });
                }
            }
        }
        await _lexBoxDbContext.SaveChangesAsync();
        return Ok(new { CreatedCount = count, UsernameConflicts = usernameConflicts });
    }
}
