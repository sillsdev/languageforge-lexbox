using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Services;
using LexCore.Auth;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexData;
using LexData.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class TestingController(
    LexAuthService lexAuthService,
    LexBoxDbContext lexBoxDbContext,
    IHgService hgService,
    SeedingData seedingData)
    : ControllerBase
{
#if DEBUG
    [AllowAnonymous]
    [HttpGet("makeJwt")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<string>> MakeJwt(string usernameOrEmail,
        UserRole userRole,
        LexboxAudience audience = LexboxAudience.LexboxApi)
    {
        var user = await lexBoxDbContext.Users.Include(u => u.Projects).ThenInclude(p => p.Project)
            .FindByEmailOrUsername(usernameOrEmail);
        if (user is null) return NotFound();
        var (token, _, _) = lexAuthService.GenerateJwt(new LexAuthUser(user) { Role = userRole, Audience = audience });
        return token;
    }

    [HttpGet("claims")]
    public Dictionary<string, string> Claims()
    {
        return User.Claims.ToLookup(c => c.Type, c => c.Value).ToDictionary(k => k.Key, v => string.Join(";", v));
    }

    [HttpPost("cleanupSeedData")]
    public async Task<ActionResult> CleanupSeedData()
    {
        await seedingData.CleanUpSeedData();
        return Ok();
    }

    [HttpGet("testTurnstile")]
    public async Task<ActionResult<bool>> TestTurnstile(string code)
    {
        return await HttpContext.RequestServices.GetRequiredService<TurnstileService>().IsTokenValid(code);
    }

    [HttpGet("debugConfiguration")]
    [AllowAnonymous]
    public ActionResult DebugConfiguration()
    {
        var configurationRoot = (IConfigurationRoot)HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        return Ok(configurationRoot.GetDebugView());
    }

#endif

    [HttpPost("seedDatabase")]
    [AdminRequired]
    public async Task<ActionResult> SeedDatabase()
    {
        await seedingData.SeedDatabase();
        return Ok();
    }

    [HttpGet("throwsException")]
    [AllowAnonymous]
    public ActionResult ThrowsException()
    {
        throw new ExceptionWithCode("This is a test exception", "test-error");
    }

    [HttpGet("test500NoException")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult Test500NoError() => StatusCode(500);

    [HttpGet("test-cleanup-reset-backups")]
    [AdminRequired]
    public async Task<string[]> TestCleanupResetBackups(bool dryRun = true)
    {
        return await hgService.CleanupResetBackups(dryRun);
    }
}
