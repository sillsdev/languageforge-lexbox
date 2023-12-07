using System.ComponentModel.DataAnnotations;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Models;
using LexBoxApi.Otel;
using LexBoxApi.Services;
using LexCore;
using LexCore.Auth;
using LexData;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

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
    private readonly TurnstileService _turnstileService;

    public LoginController(LexAuthService lexAuthService,
        LexBoxDbContext lexBoxDbContext,
        LoggedInContext loggedInContext,
        EmailService emailService,
        UserService userService,
        TurnstileService turnstileService)
    {
        _lexAuthService = lexAuthService;
        _lexBoxDbContext = lexBoxDbContext;
        _loggedInContext = loggedInContext;
        _emailService = emailService;
        _userService = userService;
        _turnstileService = turnstileService;
    }

    /// <summary>
    /// this endpoint is called when we can only pass a jwt in the query string. It redirects to the requested path
    /// and logs in using that jwt with a cookie
    /// </summary>
    [HttpGet("loginRedirect")]
    [AllowAnyAudience]

    public async Task<ActionResult> LoginRedirect(
        string jwt, // This is required because auth looks for a jwt in the query string
        string returnTo)
    {
        var user = _loggedInContext.User;
        var userUpdatedDate = await _userService.GetUserUpdatedDate(user.Id);
        if (userUpdatedDate != user.UpdatedDate)
        {
            return await EmailLinkExpired();
        }
        await HttpContext.SignInAsync(User,
            new AuthenticationProperties { IsPersistent = true });
        return Redirect(returnTo);
    }

    private async Task<ActionResult> EmailLinkExpired()
    {
        await HttpContext.SignOutAsync();
        return Redirect("/login?message=link_expired");
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
        //users can verify their email even if the updated date is out of sync when not changing their email
        //this is to prevent some edge cases where changing their name and then using an old verify email link would fail
        if (user.Email != _loggedInContext.User.Email &&
            user.UpdatedDate.ToUnixTimeSeconds() != _loggedInContext.User.UpdatedDate)
        {
            return await EmailLinkExpired();
        }

        user.Email = _loggedInContext.User.Email;
        user.EmailVerified = true;
        user.UpdateUpdatedDate();
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(Dictionary<string, string[]>))]
    [ProducesDefaultResponseType]
    public async Task<ActionResult> ForgotPassword(ForgotPasswordInput input)
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        var validToken = await _turnstileService.IsTokenValid(input.TurnstileToken, input.Email);
        activity?.AddTag("app.turnstile_token_valid", validToken);
        if (!validToken)
        {
            ModelState.AddModelError<ForgotPasswordInput>(r => r.TurnstileToken, "token invalid");
            return ValidationProblem(ModelState);
        }

        await _userService.ForgotPassword(input.Email);
        return Ok();
    }

    public record ResetPasswordRequest([Required(AllowEmptyStrings = false)] string PasswordHash);

    [HttpPost("resetPassword")]
    [RequireAudience(LexboxAudience.ForgotPassword)]
    [RequireCurrentUserInfo]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var passwordHash = request.PasswordHash;
        var lexAuthUser = _loggedInContext.User;
        var user = await _lexBoxDbContext.Users.FindAsync(lexAuthUser.Id);
        if (user == null) return NotFound();
        user.PasswordHash = PasswordHashing.HashPassword(passwordHash, user.Salt, true);
        user.UpdateUpdatedDate();
        await _lexBoxDbContext.SaveChangesAsync();
        await _emailService.SendPasswordChangedEmail(user);
        //the old jwt is only valid for calling forgot password endpoints, we need to generate a new one
        if (lexAuthUser.Audience == LexboxAudience.ForgotPassword)
            await RefreshJwt();
        return Ok();
    }
}
