using System.Security.Cryptography;
using LexBoxApi.Auth;
using LexBoxApi.Models;
using LexBoxApi.Otel;
using LexBoxApi.Services;
using LexCore;
using LexCore.Auth;
using LexCore.Entities;
using LexData;
using LexData.Entities;
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
        var validToken = await _turnstileService.IsTokenValid(accountInput.TurnstileToken, accountInput.Email);
        registerActivity?.AddTag("app.turnstile_token_valid", validToken);
        if (!validToken)
        {
            ModelState.AddModelError<RegisterAccountInput>(r => r.TurnstileToken, "token invalid");
            return ValidationProblem(ModelState);
        }

        var hasExistingUser = await _lexBoxDbContext.Users.FilterByEmailOrUsername(accountInput.Email).AnyAsync();
        registerActivity?.AddTag("app.email_available", !hasExistingUser);
        if (hasExistingUser)
        {
            ModelState.AddModelError<RegisterAccountInput>(r => r.Email, "email already in use");
            return ValidationProblem(ModelState);
        }

        var jwtUser = _loggedInContext.MaybeUser;
        var emailVerified = jwtUser?.Email == accountInput.Email;
        var createdByAdmin = jwtUser?.IsAdmin ?? false;

        var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(SHA1.HashSizeInBytes));
        var userEntity = new User
        {
            Id = Guid.NewGuid(),
            Name = accountInput.Name,
            Email = accountInput.Email,
            LocalizationCode = accountInput.Locale,
            Salt = salt,
            PasswordHash = PasswordHashing.HashPassword(accountInput.PasswordHash, salt, true),
            PasswordStrength = UserService.ClampPasswordStrength(accountInput.PasswordStrength),
            IsAdmin = false,
            EmailVerified = emailVerified,
            CreatedById = createdByAdmin ? jwtUser?.Id : null,
            Locked = false,
            CanCreateProjects = false
        };
        registerActivity?.AddTag("app.user.id", userEntity.Id);
        _lexBoxDbContext.Users.Add(userEntity);
        if (jwtUser is not null && !createdByAdmin && jwtUser.Projects.Length > 0)
        {
            userEntity.Projects = jwtUser.Projects.Select(p => new ProjectUsers { Role = p.Role, ProjectId = p.ProjectId }).ToList();
        }
        await _lexBoxDbContext.SaveChangesAsync();

        var user = new LexAuthUser(userEntity);
        if (accountInput.AutoLogin)
        {
            await HttpContext.SignInAsync(user.GetPrincipal("Registration"),
                new AuthenticationProperties { IsPersistent = true });
        }

        if (!emailVerified) await _emailService.SendVerifyAddressEmail(userEntity);
        return Ok(user);
    }

    [HttpPost("sendVerificationEmail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult> SendVerificationEmail()
    {
        var lexUser = _loggedInContext.User;
        var user = await _lexBoxDbContext.Users.FindAsync(lexUser.Id);
        if (user?.CanLogin() is not true) return NotFound();
        await _emailService.SendVerifyAddressEmail(user);
        return Ok();
    }

    [HttpGet("currentUser")]
    public ActionResult<LexAuthUser> CurrentUser()
    {
        return _loggedInContext.User;
    }
}
