using LexBoxApi.Auth;
using LexCore.Auth;
using LexCore.Entities;
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
    private readonly SeedingData _seedingData;

    public TestingController(LexAuthService lexAuthService, LexBoxDbContext lexBoxDbContext, SeedingData seedingData)
    {
        _lexAuthService = lexAuthService;
        _lexBoxDbContext = lexBoxDbContext;
        _seedingData = seedingData;
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
        return _lexAuthService.GenerateJwt(new LexAuthUser(user) { Role = userRole });
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
            project.Users.Select(u => new TestingControllerProjectUser(u.User.Username, u.Role.ToString(), u.User.Email, u.UserId))
                .ToList());
    }

    [HttpPost("cleanupSeedData")]
    public async Task<ActionResult> CleanupSeedData()
    {
        await _seedingData.CleanUpSeedData();
        return Ok();
    }

    public record TestingControllerProject(Guid Id, string Name, string Code, List<TestingControllerProjectUser> Users);

    public record TestingControllerProjectUser(string? Username, string Role, string Email, Guid Id);
}
#endif