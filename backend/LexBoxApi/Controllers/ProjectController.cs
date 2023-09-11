using LexBoxApi.Auth;
using LexBoxApi.Services;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/project")]
public class ProjectController : ControllerBase
{
    private readonly ProjectService _projectService;
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly IHgService _hgService;

    public ProjectController(ProjectService projectService, IHgService hgService, LexBoxDbContext lexBoxDbContext)
    {
        _projectService = projectService;
        _hgService = hgService;
        _lexBoxDbContext = lexBoxDbContext;
    }

    [HttpPost("refreshProjectLastChanged")]
    public async Task<ActionResult> RefreshProjectLastChanged(string projectCode)
    {
        await _projectService.UpdateLastCommit(projectCode);
        return Ok();
    }


    [HttpGet("lastCommitForRepo")]
    public async Task<ActionResult<DateTimeOffset?>> LastCommitForRepo(string code)
    {
        return await _hgService.GetLastCommitTimeFromHg(code);
    }

    [HttpPost("updateAllRepoCommitDates")]
    [AdminRequired]
    public async Task<ActionResult> UpdateAllRepoCommitDates(bool onlyUnknown)
    {
        var projectCodes = await _lexBoxDbContext.Projects.Where(p => !onlyUnknown || p.LastCommit == null).Select(p => p.Code).ToArrayAsync();
        foreach (var code in projectCodes)
        {
            await _projectService.UpdateLastCommit(code);
        }

        return Ok();
    }

    [HttpPost("autoUpdateUnknownProjectTypes")]
    [AdminRequired]
    public async Task<ActionResult<int>> AutoUpdateUnknownProjectTypes()
    {
        var updatedCount = await _lexBoxDbContext.Projects.Where(p => p.Code.EndsWith("-flex"))
            .ExecuteUpdateAsync(_ => _.SetProperty(p => p.Type, ProjectType.FLEx));
        return updatedCount;
    }

    [HttpPost("addLexboxPostfix")]
    [AdminRequired]
    public async Task<ActionResult<int>> AddLexboxSuffix()
    {
        return await _lexBoxDbContext.Projects.Where(p => !p.Code.EndsWith("-lexbox"))
            .ExecuteUpdateAsync(_ => _.SetProperty(p => p.Code, p => p.Code + "-lexbox"));
    }

    [HttpGet("backupProject/{code}")]
    [AdminRequired]
    public async Task<IActionResult> BackupProject(string code)
    {
        var filename = await _projectService.BackupProject(new Models.Project.ResetProjectByAdminInput(code));
        if (string.IsNullOrEmpty(filename)) return NotFound();
        var stream = System.IO.File.OpenRead(filename); // Do NOT use "using var stream = ..." as we need to let ASP.NET Core handle the disposal after the download completes
        return File(stream, "application/zip", filename);
    }

    [HttpPost("resetProject/{code}")]
    [AdminRequired]
    public async Task<ActionResult> ResetProject(string code)
    {
        await _projectService.ResetProject(new Models.Project.ResetProjectByAdminInput(code));
        return Ok();
    }

    [HttpDelete("project/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    [AdminRequired]
    public async Task<ActionResult<Project>> DeleteProject(Guid id)
    {
        //this project is only for testing purposes. It will delete projects permanently from the database.
        var project = await _lexBoxDbContext.Projects.FindAsync(id);
        if (project is null) return NotFound();
        if (project.RetentionPolicy != RetentionPolicy.Dev) return Forbid();
        _lexBoxDbContext.Projects.Remove(project);
        var hgService = HttpContext.RequestServices.GetRequiredService<IHgService>();
        await hgService.DeleteRepo(project.Code);
        await _lexBoxDbContext.SaveChangesAsync();
        return project;
    }
}
