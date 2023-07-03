using LexBoxApi.Auth;
using LexCore;
using LexCore.Auth;
using LexData;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/admin")]
public class AdminController : ControllerBase
{
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly LoggedInContext _loggedInContext;

    public AdminController(LexBoxDbContext lexBoxDbContext,
        LoggedInContext loggedInContext)
    {
        _lexBoxDbContext = lexBoxDbContext;
        _loggedInContext = loggedInContext;
    }

    public record ResetPasswordAdminRequest(string PasswordHash, Guid userId);

    [HttpPost("resetPassword")]
    [AdminRequired]
    public async Task<ActionResult> ResetPasswordAdmin(ResetPasswordAdminRequest request)
    {
        var passwordHash = request.PasswordHash;
        var lexAuthUser = _loggedInContext.User;
        var user = await _lexBoxDbContext.Users.FirstAsync(u => u.Id == request.userId);
        user.PasswordHash = PasswordHashing.HashPassword(passwordHash, user.Salt, true);
        await _lexBoxDbContext.SaveChangesAsync();
        return Ok();
    }
}
