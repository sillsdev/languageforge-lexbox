using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Services;
using LexCore.Auth;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexData;
using LexData.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class TestingController(
    LexAuthService lexAuthService,
    LexBoxDbContext lexBoxDbContext,
    IHgService hgService,
    SeedingData seedingData,
    ProjectService projectService,
    LoggedInContext loggedInContext,
    IOpenIddictAuthorizationManager? authorizationManager = null,
    IOpenIddictApplicationManager? applicationManager = null
    )
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
        LexboxAudience audience = LexboxAudience.LexboxApi,
        string? scopes = nameof(LexboxAuthScope.LexboxApi))
    {
        var user = await lexBoxDbContext.Users.Include(u => u.Projects).ThenInclude(p => p.Project)
            .FindByEmailOrUsername(usernameOrEmail);
        if (user is null) return NotFound();
        var lexAuthUser = new LexAuthUser(user) { Role = userRole, Audience = audience };
        if (!string.IsNullOrEmpty(scopes)) lexAuthUser.ScopeString = scopes.ToLower();
        var token = lexAuthService.GenerateJwt(lexAuthUser, TimeSpan.FromMinutes(30));
        return token;
    }

    [HttpGet("claims")]
    [AllowAnonymous]
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

    [HttpPost("copyToNewProject")]
    [AdminRequired]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<Guid>> CopyToNewProject(string newProjectCode, string existingProjectCode)
    {
        var id = Guid.NewGuid();
        if (!await projectService.ProjectExists(existingProjectCode)) return NotFound("Existing project code not found");
        if (await projectService.ProjectExists(newProjectCode)) return BadRequest("New project code already in use");

        await projectService.CreateProject(new(id,
            newProjectCode,
            "Copy of " + existingProjectCode,
            newProjectCode,
            ProjectType.FLEx,
            RetentionPolicy.Dev,
            true,
            loggedInContext.User.Id,
            null));
        await hgService.CopyRepo(existingProjectCode, newProjectCode);
        return Ok(id);
    }

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

    [HttpPost("pre-approve-oauth-app")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> PreApproveOauthApp(string clientId, string scopes)
    {
        if (authorizationManager is null) throw new InvalidOperationException("authorizationManager is null");
        if (applicationManager is null) throw new InvalidOperationException("applicationManager is null");
        var application = await applicationManager.FindByClientIdAsync(clientId);
        if (application is null)
            return NotFound("unable to find a registered application with the client id " + clientId);
        var applicationId = await applicationManager.GetIdAsync(application);
        if (applicationId is null) throw new InvalidOperationException("applicationId is null");
        await authorizationManager.CreateAsync(
            principal: User,
            subject: loggedInContext.User.Id.ToString(),
            client: applicationId,
            type: OpenIddictConstants.AuthorizationTypes.Permanent,
            scopes: [..scopes.Split(' ')]);
        return Ok();
    }
}
