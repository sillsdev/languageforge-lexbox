using System.Security.Cryptography;
using LexBoxApi.Auth;
using LexBoxApi.Models;
using LexBoxApi.Otel;
using LexBoxApi.Services;
using LexCore;
using LexCore.Auth;
using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class UserController : ControllerBase
{
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly TurnstileService _turnstileService;
    private readonly LoggedInContext _loggedInContext;
    private readonly EmailService _emailService;
    private readonly LexAuthService _lexAuthService;

    public UserController(
        LexBoxDbContext lexBoxDbContext,
        TurnstileService turnstileService,
        LoggedInContext loggedInContext,
        EmailService emailService,
        LexAuthService lexAuthService
    )
    {
        _lexBoxDbContext = lexBoxDbContext;
        _turnstileService = turnstileService;
        _loggedInContext = loggedInContext;
        _emailService = emailService;
        _lexAuthService = lexAuthService;
    }

    [HttpPost("registerAccount")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(Dictionary<string, string[]>))]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<LexAuthUser>> RegisterAccount(RegisterAccountInput accountInput)
    {
        using var registerActivity = LexBoxActivitySource.Get().StartActivity("Register");
        var validToken = await _turnstileService.IsTokenValid(accountInput.TurnstileToken);
        registerActivity?.AddTag("app.turnstile_token_valid", validToken);
        if (!validToken)
        {
            ModelState.AddModelError<RegisterAccountInput>(r => r.TurnstileToken, "token invalid");
            return ValidationProblem(ModelState);
        }

        var hasExistingUser = await _lexBoxDbContext.Users.AnyAsync(u => u.Email == accountInput.Email);
        registerActivity?.AddTag("app.email_available", !hasExistingUser);
        if (hasExistingUser)
        {
            ModelState.AddModelError<RegisterAccountInput>(r => r.Email, "email already in use");
            return ValidationProblem(ModelState);
        }

        var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(SHA1.HashSizeInBytes));
        var userEntity = new User
        {
            Id = Guid.NewGuid(),
            Name = accountInput.Name,
            Email = accountInput.Email,
            Salt = salt,
            PasswordHash = PasswordHashing.HashPassword(accountInput.PasswordHash, salt, true),
            IsAdmin = false,
            EmailVerified = false,
        };
        registerActivity?.AddTag("app.user.id", userEntity.Id);
        _lexBoxDbContext.Users.Add(userEntity);
        await _lexBoxDbContext.SaveChangesAsync();

        var user = new LexAuthUser(userEntity);
        await HttpContext.SignInAsync(user.GetPrincipal("Registration"),
            new AuthenticationProperties { IsPersistent = true });

        var (jwt, _) = _lexAuthService.GenerateJwt(user);
        await _emailService.SendVerifyAddressEmail(jwt, userEntity);

        return Ok(user);
    }

    [HttpPost("sendVerificationEmail")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult> SendVerificationEmail()
    {
        var lexUser = _loggedInContext.User;
        var user = _lexBoxDbContext.Users.Find(lexUser.Id);
        if (user is null) return Unauthorized();
        var (jwt, _) = _lexAuthService.GenerateJwt(new LexAuthUser(user)
        {
            EmailVerificationRequired = null,
        });
        await _emailService.SendVerifyAddressEmail(jwt, user);
        return Ok();
    }

    [HttpGet("currentUser")]
    public ActionResult<LexAuthUser> CurrentUser()
    {
        return _loggedInContext.User;
    }
}
