using LexCore;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Controllers;

[ApiController]
public class LoginController: ControllerBase
{
    private readonly LexBoxDbContext _lexBoxDbContext;

    public LoginController(LexBoxDbContext lexBoxDbContext)
    {
        _lexBoxDbContext = lexBoxDbContext;
    }

    [HttpPost("login")]
    public async Task<ActionResult<bool>> Login(string usernameOrEmail, string pw)
    {
        var user = await _lexBoxDbContext.Users.FirstOrDefaultAsync(user => user.Email == usernameOrEmail || user.Username == usernameOrEmail);
        if (user is null) return NotFound();
        return PasswordHashing.RedminePasswordHash(pw, user.Salt) == user.PasswordHash;
    }

    [HttpGet("hashPassword")]
    public ActionResult<string> HashPassword(string pw, string salt)
    {
        return PasswordHashing.RedminePasswordHash(pw, salt);
    }
}