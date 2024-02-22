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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using LexCore.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/login")]
public class LoginController(
    LexAuthService lexAuthService,
    LexBoxDbContext lexBoxDbContext,
    LoggedInContext loggedInContext,
    GoogleTokenValidator googleTokenValidator,
    EmailService emailService,
    UserService userService,
    TurnstileService turnstileService,
    ProjectService projectService)
    : ControllerBase
{
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
        var user = loggedInContext.User;
        // A RegisterAccount token means there's no user account yet, so checking UpdatedDate makes no sense
        if (user.Audience != LexboxAudience.RegisterAccount)
        {
            var userUpdatedDate = await userService.GetUserUpdatedDate(user.Id);
            if (userUpdatedDate != user.UpdatedDate)
            {
                return await EmailLinkExpired();
            }
        }
        await HttpContext.SignInAsync(User,
            new AuthenticationProperties { IsPersistent = true });
        return Redirect(returnTo);
    }

    [HttpGet("google")]
    [AllowAnonymous]
    public IActionResult GoogleLogin(string? redirectTo = null)
    {
        if (string.IsNullOrEmpty(redirectTo)) redirectTo = "/home";
        var authProps = new AuthenticationProperties { RedirectUri = redirectTo };
        // If we want, we could look for expired-but-otherwise-valid JWT cookies and extract the user's email address, then:
        // authProps.SetParameter("login_hint", email);
        return Challenge(authProps, GoogleDefaults.AuthenticationScheme);
    }

    public static async Task<string> CompleteGoogleLogin(
        ClaimsPrincipal? principal,
        string? returnTo,
        HttpContext httpContext,
        LexAuthService lexAuthService
        )
    {
        returnTo ??= "/home";
        var googleEmail = principal?.FindFirstValue(ClaimTypes.Email);
        var googleName = principal?.FindFirstValue(ClaimTypes.Name);
        var locale = principal?.FindFirstValue("locale");
        var (authUser, _) = await lexAuthService.GetUser(googleEmail);
        if (authUser is null)
        {
            authUser = new LexAuthUser()
            {
                Id = Guid.NewGuid(),
                Audience = LexboxAudience.RegisterAccount,
                Name = googleName ?? "",
                Email = googleEmail,
                EmailVerificationRequired = null,
                Role = UserRole.user,
                UpdatedDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
                CanCreateProjects = null,
                Locale = locale ?? LexCore.Entities.User.DefaultLocalizationCode,
                Locked = null,
            };
            var queryParams = new Dictionary<string, string?>() {
                {"email", googleEmail},
                {"name", googleName},
                {"returnTo", returnTo},
            };
            var queryString = QueryString.Create(queryParams);
            returnTo = "/register" + queryString.ToString();
        }
        await httpContext.SignInAsync(authUser.GetPrincipal("google"),
            new AuthenticationProperties { IsPersistent = true });
        return returnTo;
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
        if (loggedInContext.User.EmailVerificationRequired == true)
        {
            return Unauthorized();
        }

        var userId = loggedInContext.User.Id;
        var user = await lexBoxDbContext.Users.FindAsync(userId);
        if (user == null) return NotFound();
        //users can verify their email even if the updated date is out of sync when not changing their email
        //this is to prevent some edge cases where changing their name and then using an old verify email link would fail
        if (user.Email != loggedInContext.User.Email &&
            user.UpdatedDate.ToUnixTimeSeconds() != loggedInContext.User.UpdatedDate)
        {
            return await EmailLinkExpired();
        }

        user.Email = loggedInContext.User.Email;
        user.EmailVerified = true;
        user.UpdateUpdatedDate();
        await lexBoxDbContext.SaveChangesAsync();
        await RefreshJwt();
        return Redirect(returnTo);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [AllowAnonymous]
    public async Task<ActionResult<LexAuthUser>> Login(LoginRequest loginRequest)
    {
        var (user, error) = await lexAuthService.Login(loginRequest);

        if (error is not null)
        {
            return Unauthorized(error.ToString());
        }
        else if (user is null)
        {
            return Unauthorized();
        }

        await userService.UpdateUserLastActive(user.Id);
        await HttpContext.SignInAsync(user.GetPrincipal("Password"),
            new AuthenticationProperties { IsPersistent = true });
        return user;
    }

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LexAuthUser>> RefreshJwt()
    {
        var user = await lexAuthService.RefreshUser(loggedInContext.User.Id);
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
        var validToken = await turnstileService.IsTokenValid(input.TurnstileToken, input.Email);
        activity?.AddTag("app.turnstile_token_valid", validToken);
        if (!validToken)
        {
            ModelState.AddModelError<ForgotPasswordInput>(r => r.TurnstileToken, "token invalid");
            return ValidationProblem(ModelState);
        }

        await userService.ForgotPassword(input.Email);
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
        var lexAuthUser = loggedInContext.User;
        var user = await lexBoxDbContext.Users.FindAsync(lexAuthUser.Id);
        if (user == null) return NotFound();
        user.PasswordHash = PasswordHashing.HashPassword(passwordHash, user.Salt, true);
        user.UpdateUpdatedDate();
        await lexBoxDbContext.SaveChangesAsync();
        await emailService.SendPasswordChangedEmail(user);
        //the old jwt is only valid for calling forgot password endpoints, we need to generate a new one
        if (lexAuthUser.Audience == LexboxAudience.ForgotPassword)
            await RefreshJwt();
        return Ok();
    }
}
