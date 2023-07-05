using LexBoxApi.Auth;
using LexBoxApi.Services;
using LexCore.Auth;
using LexData;
using LexData.Redmine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Controllers;

#if DEBUG
[ApiController]
[Route("/api/[controller]")]
public class TestingController : ControllerBase
{
    private readonly LexAuthService _lexAuthService;
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly SeedingData _seedingData;
    private readonly RedmineDbContext _redmineDbContext;

    public TestingController(LexAuthService lexAuthService,
        LexBoxDbContext lexBoxDbContext,
        SeedingData seedingData,
        RedmineDbContext redmineDbContext)
    {
        _lexAuthService = lexAuthService;
        _lexBoxDbContext = lexBoxDbContext;
        _seedingData = seedingData;
        _redmineDbContext = redmineDbContext;
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
        var (token, _) = _lexAuthService.GenerateJwt(new LexAuthUser(user) { Role = userRole });
        return token;
    }

    [HttpPost("seedDatabase")]
    public async Task<ActionResult<TestingControllerProject>> SeedDatabase()
    {
        await _seedingData.SeedDatabase();
        var project = await _lexBoxDbContext.Projects
            .Include(p => p.Users)
            .ThenInclude(u => u.User)
            .FirstOrDefaultAsync(p => p.Code == "sena-3");
        ArgumentNullException.ThrowIfNull(project);
        return new TestingControllerProject(project.Id,
            project.Name,
            project.Code,
            project.Users.Select(u =>
                    new TestingControllerProjectUser(u.User.Username, u.Role.ToString(), u.User.Email, u.UserId))
                .ToList());
    }

    [HttpPost("cleanupSeedData")]
    public async Task<ActionResult> CleanupSeedData()
    {
        await _seedingData.CleanUpSeedData();
        return Ok();
    }

    [HttpGet("testTurnstile")]
    public async Task<ActionResult<bool>> testTurnstile(string code)
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

    [HttpGet("listRedmineUsernames")]
    public async Task<ActionResult<RmUser[]>> ListRedmineUsernames()
    {
        return await _redmineDbContext.Users.Take(100).ToArrayAsync();
    }

    public record TestingControllerProject(Guid Id, string Name, string Code, List<TestingControllerProjectUser> Users);

    public record TestingControllerProjectUser(string? Username, string Role, string Email, Guid Id);
}
#endif
