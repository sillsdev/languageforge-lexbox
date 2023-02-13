using System.Security.Cryptography;
using System.Text;
using LexCore;
using Microsoft.AspNetCore.Mvc;

namespace LexBoxApi.Controllers;

[ApiController]
public class LoginController: ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<bool>> Login(string user, string pw)
    {
        return false;
    }

    [HttpGet("hashPassword")]
    public ActionResult<string> HashPassword(string pw, string salt)
    {
        return PasswordHashing.RedminePasswordHash(pw, salt);
    }
}