using LexBoxApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/project")]
public class ProjectController: ControllerBase
{
    private readonly ProjectService _projectService;

    public ProjectController(ProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpPost("refreshProjectLastChanged")]
    public async Task<ActionResult> RefreshProjectLastChanged(string projectCode)
    {
        await _projectService.UpdateLastCommit(projectCode);
        return Ok();
    }
}