using LexBoxApi.Auth;
using LexBoxApi.Services;
using LexCore.Entities;
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
}
