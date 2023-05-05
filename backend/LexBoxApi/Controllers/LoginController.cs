using LexBoxApi.Auth;
using LexBoxApi.Services;
using LexCore;
using LexCore.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/login")]
public class LoginController : ControllerBase
{
    private readonly LexAuthService _lexAuthService;
    private readonly EmailService _emailService;

    public LoginController(LexAuthService lexAuthService, EmailService emailService)
    {
        _lexAuthService = lexAuthService;
        _emailService = emailService;
    }

    [HttpGet("loginRedirect")]
    [Authorize]
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
    public async Task<ActionResult> ForgotPassword(string email)
    {
        await _lexAuthService.ForgotPassword(email);
        return Ok();
    }

    [HttpPost("resetPassword")]
    public async Task<ActionResult> ResetPassword(string email, string token, string newPassword)
    {
        return Ok();
    }
}