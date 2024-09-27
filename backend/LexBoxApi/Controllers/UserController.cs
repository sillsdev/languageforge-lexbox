using System.Security.Cryptography;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Models;
using LexBoxApi.Otel;
using LexBoxApi.Services;
using LexBoxApi.Services.Email;
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
    private readonly IEmailService _emailService;
    private readonly LexAuthService _lexAuthService;

    public UserController(
        LexBoxDbContext lexBoxDbContext,
        TurnstileService turnstileService,
        LoggedInContext loggedInContext,
        IEmailService emailService,
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

        var userEntity = CreateUserEntity(accountInput, jwtUser);
        registerActivity?.AddTag("app.user.id", userEntity.Id);
        _lexBoxDbContext.Users.Add(userEntity);
        await _lexBoxDbContext.SaveChangesAsync();

        var user = new LexAuthUser(userEntity);
        await HttpContext.SignInAsync(user.GetPrincipal("Registration"),
            new AuthenticationProperties { IsPersistent = true });

        if (!userEntity.EmailVerified) await _emailService.SendVerifyAddressEmail(userEntity);
        return Ok(user);
    }

    [HttpGet("acceptInvitation")]
    [RequireAudience(LexboxAudience.RegisterAccount, true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> HandleInviteLink(LoggedInContext loggedInContext, LexBoxDbContext dbContext)
    {
        var user = loggedInContext.User;
        if (user.Email is null)
        {
            // Malformed JWT, exit early
            return Redirect("/login");
        }
        var dbUser = await dbContext.Users
            .Where(u => u.Email == user.Email)
            .Include(u => u.Projects)
            .Include(u => u.Organizations)
            .FirstOrDefaultAsync();
        if (dbUser is null)
        {
            // Go to frontend to fill out registration page
            var queryString = QueryString.Create("email", user.Email);
            var returnTo = new UriBuilder { Path = "/acceptInvitation", Query = queryString.Value }.Uri.PathAndQuery;
            return Redirect(returnTo);
        }
        else
        {
            // No need to re-register users that already exist
            UpdateUserMemberships(user, dbUser);
            await dbContext.SaveChangesAsync();
            var loginUser = new LexAuthUser(dbUser);
            await HttpContext.SignInAsync(loginUser.GetPrincipal("Invitation"),
                new AuthenticationProperties { IsPersistent = true });
            return Redirect("/");
        }
    }

    [HttpPost("acceptInvitation")]
    [RequireAudience(LexboxAudience.RegisterAccount, true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(Dictionary<string, string[]>))]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<LexAuthUser>> AcceptEmailInvitation(RegisterAccountInput accountInput)
    {
        using var acceptActivity = LexBoxActivitySource.Get().StartActivity("AcceptInvitation");
        var validToken = await _turnstileService.IsTokenValid(accountInput.TurnstileToken, accountInput.Email);
        acceptActivity?.AddTag("app.turnstile_token_valid", validToken);
        if (!validToken)
        {
            ModelState.AddModelError<RegisterAccountInput>(r => r.TurnstileToken, "token invalid");
            return ValidationProblem(ModelState);
        }

        var jwtUser = _loggedInContext.User;
        if (jwtUser.Email != accountInput.Email)
        {
            // Changing email address in invite links is not allowed; this prevents someone from trying to reuse a JWT belonging to somebody else
            ModelState.AddModelError<RegisterAccountInput>(r => r.Email, "email address mismatch in invitation link");
            return ValidationProblem(ModelState);
        }

        var userEntity = await _lexBoxDbContext.Users.FindByEmailOrUsername(accountInput.Email);
        acceptActivity?.AddTag("app.email_available", userEntity is null);
        if (userEntity is null)
        {
            userEntity = CreateUserEntity(accountInput, jwtUser);
            _lexBoxDbContext.Users.Add(userEntity);
        }
        else
        {
            // Multiple invitations accepted by the same account should no longer go through this method, so return an error if the account already exists
            // That can only happen if an admin created the user's account while the user was still on the registration page: very unlikely
            ModelState.AddModelError<RegisterAccountInput>(r => r.Email, "email already in use");
            return ValidationProblem(ModelState);
        }

        acceptActivity?.AddTag("app.user.id", userEntity.Id);
        await _lexBoxDbContext.SaveChangesAsync();

        var user = new LexAuthUser(userEntity);
        await HttpContext.SignInAsync(user.GetPrincipal("Registration"),
            new AuthenticationProperties { IsPersistent = true });

        if (!userEntity.EmailVerified) await _emailService.SendVerifyAddressEmail(userEntity);
        return Ok(user);
    }

    private User CreateUserEntity(RegisterAccountInput input, LexAuthUser? jwtUser, Guid? creatorId = null)
    {
        var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(SHA1.HashSizeInBytes));
        var userEntity = new User
        {
            Id = Guid.NewGuid(),
            Name = input.Name,
            Email = input.Email,
            LocalizationCode = input.Locale,
            Salt = salt,
            PasswordHash = PasswordHashing.HashPassword(input.PasswordHash, salt, true),
            PasswordStrength = UserService.ClampPasswordStrength(input.PasswordStrength),
            IsAdmin = false,
            EmailVerified = jwtUser?.Email == input.Email,
            CreatedById = creatorId,
            Locked = false,
            CanCreateProjects = false
        };
        UpdateUserMemberships(jwtUser, userEntity);
        return userEntity;
    }
    private void UpdateUserMemberships(LexAuthUser? jwtUser, User userEntity)
    {
        // This audience check is redundant now because of [RequireAudience(LexboxAudience.RegisterAccount, true)], but let's leave it in for safety
        if (jwtUser?.Audience == LexboxAudience.RegisterAccount && jwtUser.Projects.Length > 0)
        {
            foreach (var p in jwtUser.Projects)
            {
                if (!userEntity.Projects.Exists(proj => proj.ProjectId == p.ProjectId))
                {
                    userEntity.Projects.Add(new ProjectUsers { Role = p.Role, ProjectId = p.ProjectId });
                }
            }
        }
        if (jwtUser?.Audience == LexboxAudience.RegisterAccount && jwtUser.Orgs.Length > 0)
        {
            foreach (var o in jwtUser.Orgs)
            {
                if (!userEntity.Organizations.Exists(org => org.OrgId == o.OrgId))
                {
                    userEntity.Organizations.Add(new OrgMember { Role = o.Role, OrgId = o.OrgId });
                }
            }
        }
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
