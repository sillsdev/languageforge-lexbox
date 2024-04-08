using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Services;
using LexCore.Auth;
using LexCore.Config;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/integration")]
public class IntegrationController(
    LexBoxDbContext lexBoxDbContext,
    LexAuthService authService,
    LoggedInContext loggedInContext,
    IHgService hgService,
    IPermissionService permissionService,
    IOptions<HgConfig> hgConfigOptions,
    IHostEnvironment hostEnvironment,
    ProjectService projectService)
    : ControllerBase
{
    private readonly string _protocol = hostEnvironment.IsDevelopment() ? "http" : "https";
    private readonly HgConfig _hgConfig = hgConfigOptions.Value;

    [HttpGet("openWithFlex")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<ActionResult> OpenWithFlex(Guid projectId)
    {
        if (!permissionService.CanAccessProject(projectId)) return Unauthorized();
        var project = await lexBoxDbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null) return NotFound();
        var repoId = await hgService.GetRepositoryIdentifier(project);
        var (projectToken, _, flexToken, _) = GetRefreshResponse(projectId);
        var projectUri = $"{_protocol}://{_hgConfig.SendReceiveDomain}/{project.Code}";
        var queryString = new QueryString()
            .Add("db", project.Code)
            .Add("user", "bearer")
            .Add("password", projectToken)
            .Add("flexRefreshToken", flexToken)
            .Add("projectUri", projectUri)
            //could be empty if the repo hasn't been pushed to yet
            .Add("repositoryIdentifier", repoId ?? "");
        return Redirect($"silfw://localhost/link{queryString.Value}");
    }

    [HttpGet("getProjectToken")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    //todo make exclusive to prevent calling with normal jwt, currently used for testing
    [RequireAudience(LexboxAudience.SendAndReceiveRefresh)]
    public async Task<ActionResult<RefreshResponse>> GetProjectToken(string projectCode)
    {
        var projectId = await projectService.LookupProjectId(projectCode);
        if (projectId == default) return NotFound();
        return GetRefreshResponse(projectId);
    }

    private RefreshResponse GetRefreshResponse(Guid projectId)
    {
        var user = loggedInContext.User;
        //generates a short lived token only useful for S&R of this one project
        var (projectToken, projectTokenExpiresAt, _) = authService.GenerateJwt(
            user with
            {
                Projects = user.Projects.Where(p => p.ProjectId == projectId).ToArray(),
                Audience = LexboxAudience.SendAndReceive,
            });

        //refresh long lived token used to get new tokens
        var (flexToken, flexTokenExpiresAt, _) = authService.GenerateJwt(user with
        {
            Projects = [],
            Audience = LexboxAudience.SendAndReceiveRefresh,
        });
        return new RefreshResponse(projectToken, projectTokenExpiresAt, flexToken, flexTokenExpiresAt);
    }
}
