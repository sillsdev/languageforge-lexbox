using LexBoxApi.Auth;
using LexBoxApi.Models.Project;
using LexBoxApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class ProjectController: ControllerBase
{
    private readonly LoggedInContext _loggedInContext;
    private readonly ProjectService _projectService;

    public ProjectController(LoggedInContext loggedInContext, ProjectService projectService)
    {
        _loggedInContext = loggedInContext;
        _projectService = projectService;
    }

    [HttpPost("createProject")]
    public async Task<ActionResult<Guid>> CreateProject(CreateProjectModel model)
    {
        return await _projectService.CreateProject(model, _loggedInContext.User.Id);
    }
}