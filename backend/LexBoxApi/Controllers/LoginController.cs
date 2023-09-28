using System.ComponentModel.DataAnnotations;
using LexBoxApi.Auth;
using LexBoxApi.Services;
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
    private readonly EmailService _emailService;
    private readonly UserService _userService;

    public LoginController(LexAuthService lexAuthService,
        LexBoxDbContext lexBoxDbContext,
        LoggedInContext loggedInContext,
        EmailService emailService,
        UserService userService)
    {
        _lexAuthService = lexAuthService;
        _lexBoxDbContext = lexBoxDbContext;
        _loggedInContext = loggedInContext;
        _emailService = emailService;
        _userService = userService;
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

    [HttpGet("verifyEmail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LexAuthUser>> VerifyEmail(
        string jwt, // This is required because auth looks for a jwt in the query string
        string returnTo)
    {
        if (_loggedInContext.User.EmailVerificationRequired == true)
        {
            return Unauthorized();
        }

        var userId = _loggedInContext.User.Id;
        var user = await _lexBoxDbContext.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.Email = _loggedInContext.User.Email;
        user.EmailVerified = true;
        await _lexBoxDbContext.SaveChangesAsync();
        await RefreshJwt();
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
        await _userService.UpdateUserLastActive(user.Id);
        await HttpContext.SignInAsync(user.GetPrincipal("Password"),
            new AuthenticationProperties { IsPersistent = true });
        return user;
    }

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LexAuthUser>> RefreshJwt()
    {
        var user = await _lexAuthService.RefreshUser(_loggedInContext.User.Id);
        if (user == null) return Unauthorized();
        return user;
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }

    [HttpPost("forgotPassword")]
    [AllowAnonymous]
    public async Task<ActionResult> ForgotPassword(string email)
    {
        await _lexAuthService.ForgotPassword(email);
        return Ok();
    }

    public record ResetPasswordRequest([Required(AllowEmptyStrings = false)] string PasswordHash);

    [HttpPost("resetPassword")]
    public async Task<ActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var passwordHash = request.PasswordHash;
        var lexAuthUser = _loggedInContext.User;
        var user = await _lexBoxDbContext.Users.FirstAsync(u => u.Id == lexAuthUser.Id);
        user.PasswordHash = PasswordHashing.HashPassword(passwordHash, user.Salt, true);
        await _lexBoxDbContext.SaveChangesAsync();
        await _emailService.SendPasswordChangedEmail(user);
        return Ok();
    }
}
