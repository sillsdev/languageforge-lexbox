using System.Security.Cryptography;
using LexBoxApi.Models;
using LexBoxApi.Otel;
using LexBoxApi.Services;
using LexCore;
using LexCore.Auth;
using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Authentication;
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

    public UserController(LexBoxDbContext lexBoxDbContext, TurnstileService turnstileService)
    {
        _lexBoxDbContext = lexBoxDbContext;
        _turnstileService = turnstileService;
    }

    [HttpPost("registerAccount")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(Dictionary<string, string[]>))]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<LexAuthUser>> RegisterAccount(RegisterAccountInput accountInput)
    {
        using var registerActivity = LexBoxActivitySource.Get().StartActivity("Register");
        Console.WriteLine("RegisterAccount" + registerActivity?.DisplayName);
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
            IsAdmin = false
        };
        registerActivity?.AddTag("app.user.id", userEntity.Id);
        _lexBoxDbContext.Users.Add(userEntity);
        await _lexBoxDbContext.SaveChangesAsync();

        var user = new LexAuthUser(userEntity);
        await HttpContext.SignInAsync(user.GetPrincipal("Registration"),
            new AuthenticationProperties { IsPersistent = true });
        return Ok(user);
    }
}