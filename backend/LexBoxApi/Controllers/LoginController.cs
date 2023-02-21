using LexBoxApi.Auth;
using LexCore;
using LexData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

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

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [AllowAnonymous]
    public async Task<ActionResult<string>> Login(string usernameOrEmail, [FromForm] string pw)
    {
        var user = await _lexAuthService.Login(usernameOrEmail, pw);
        if (user == null) return Unauthorized();
        return _lexAuthService.GenerateJwt(user);
    }

    [HttpGet("hashPassword")]
    public ActionResult<string> HashPassword(string pw, string salt)
    {
        return PasswordHashing.RedminePasswordHash(pw, salt);
    }
}