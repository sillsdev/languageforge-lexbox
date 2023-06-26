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
[Route("/api/login")]
public class LoginController : ControllerBase
{
    private readonly LexAuthService _lexAuthService;
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly LoggedInContext _loggedInContext;

    public LoginController(LexAuthService lexAuthService,
        LexBoxDbContext lexBoxDbContext,
        LoggedInContext loggedInContext)
    {
        _lexAuthService = lexAuthService;
        _lexBoxDbContext = lexBoxDbContext;
        _loggedInContext = loggedInContext;
    }

    [HttpGet("loginRedirect")]
    public async Task<ActionResult> LoginRedirect(
        string jwt, // This is required because auth looks for a jwt in the query string
        string returnTo)
    {
        await HttpContext.SignInAsync(User,
            new AuthenticationProperties { IsPersistent = true });
        return Redirect(returnTo);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [AllowAnonymous]
    public async Task<ActionResult<LexAuthUser>> Login(LoginRequest loginRequest)
    {
        var user = await _lexAuthService.Login(loginRequest);
        if (user == null) return Unauthorized();
        await HttpContext.SignInAsync(user.GetPrincipal("Password"),
            new AuthenticationProperties { IsPersistent = true });
        return user;
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }

    [HttpGet("hashPassword")]
    [AdminRequired]
    public ActionResult<string> HashPassword(string pw, string salt)
    {
        return PasswordHashing.RedminePasswordHash(pw, salt, false);
    }

    [HttpPost("forgotPassword")]
    [AllowAnonymous]
    public async Task<ActionResult> ForgotPassword(string email)
    {
        await _lexAuthService.ForgotPassword(email);
        return Ok();
    }

    public record ResetPasswordRequest(string PasswordHash);

    [HttpPost("resetPassword")]
    public async Task<ActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var passwordHash = request.PasswordHash;
        var lexAuthUser = _loggedInContext.User;
        var user = await _lexBoxDbContext.Users.FirstAsync(u => u.Id == lexAuthUser.Id);
        user.PasswordHash = PasswordHashing.HashPassword(passwordHash, user.Salt, true);
        await _lexBoxDbContext.SaveChangesAsync();
        return Ok();
    }

    public record ResetPasswordAdminRequest(string PasswordHash, Guid userId);

    [HttpPost("resetPasswordAdmin")]
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
