using System.ComponentModel.DataAnnotations;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Services;
using LexCore;
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
    private readonly UserService _userService;
    private readonly EmailService _emailService;

    public AdminController(LexBoxDbContext lexBoxDbContext,
        LoggedInContext loggedInContext,
        UserService userService,
        EmailService emailService
    )
    {
        _lexBoxDbContext = lexBoxDbContext;
        _loggedInContext = loggedInContext;
        _userService = userService;
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
        await _userService.ResetPasswordStrength(user.Id);
        user.UpdateUpdatedDate();
        await _lexBoxDbContext.SaveChangesAsync();
        await _emailService.SendPasswordChangedEmail(user);
        return Ok();
    }
}
