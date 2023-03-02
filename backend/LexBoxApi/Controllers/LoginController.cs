using System.Security.Claims;
using LexBoxApi.Auth;
using LexBoxApi.Models;
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

    public LoginController(LexAuthService lexAuthService)
    {
        _lexAuthService = lexAuthService;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [AllowAnonymous]
    public async Task<ActionResult<LexAuthUser>> Login(LoginRequest loginRequest)
    {
        var user = await _lexAuthService.Login(loginRequest);
        if (user == null) return Unauthorized();
        await HttpContext.SignInAsync(user.GetPrincipal("Password"), new AuthenticationProperties
        {
            IsPersistent = true
        });
        return user;
    }

    [HttpGet("hashPassword")]
    public ActionResult<string> HashPassword(string pw, string salt)
    {
        return PasswordHashing.RedminePasswordHash(pw, salt, false);
    }
}