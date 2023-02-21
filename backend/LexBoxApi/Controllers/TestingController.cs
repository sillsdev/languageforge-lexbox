using LexBoxApi.Auth;
using LexCore.Auth;
using LexData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace LexBoxApi.Controllers;

#if DEBUG
[ApiController]
public class TestingController : ControllerBase
{
    private readonly LexAuthService _lexAuthService;
    private readonly LexBoxDbContext _lexBoxDbContext;

    public TestingController(LexAuthService lexAuthService, LexBoxDbContext lexBoxDbContext)
    {
        _lexAuthService = lexAuthService;
        _lexBoxDbContext = lexBoxDbContext;
    }

    [HttpGet("requires-auth")]
    public OkObjectResult RequiresAuth()
    {
        return Ok("success: " + User.Identity?.Name ?? "Unknown");
    }

    [HttpGet("requires-admin")]
    [AdminRequired]
    public OkResult RequiresAdmin()
    {
        return Ok();
    }

    [AllowAnonymous]
    [HttpGet("makeJwt")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<string>> MakeJwt(string usernameOrEmail, UserRole userRole)
    {
        var user = await _lexBoxDbContext.Users.Include(u => u.Projects).ThenInclude(p => p.Project)
            .FirstOrDefaultAsync(user => user.Email == usernameOrEmail || user.Username == usernameOrEmail);
        if (user is null) return NotFound();
        return _lexAuthService.GenerateJwt(new LexAuthUser(user) {Role = userRole});
    }
}
#endif