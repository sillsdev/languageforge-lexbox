using System.Collections.Generic;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
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
    private readonly IPermissionService _permissionService;

    public ProjectController(ProjectService projectService, IHgService hgService, LexBoxDbContext lexBoxDbContext, IPermissionService permissionService)
    {
        _projectService = projectService;
        _hgService = hgService;
        _lexBoxDbContext = lexBoxDbContext;
        _permissionService = permissionService;
    }

    [HttpPost("refreshProjectLastChanged")]
    public async Task<ActionResult> RefreshProjectLastChanged(string projectCode)
    {
        await _projectService.UpdateLastCommit(projectCode);
        return Ok();
    }


    [HttpGet("lastCommitForRepo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<DateTimeOffset?>> LastCommitForRepo(string code)
    {
        var migrationStatus = await _lexBoxDbContext.Projects.Where(p => p.Code == code).Select(p => p.MigrationStatus).FirstOrDefaultAsync();
        if (migrationStatus == default) return NotFound();
        return await _hgService.GetLastCommitTimeFromHg(code, migrationStatus);
    }

    [HttpPost("updateAllRepoCommitDates")]
    [AdminRequired]
    public async Task<ActionResult> UpdateAllRepoCommitDates(bool onlyUnknown)
    {
        var projects = _lexBoxDbContext.Projects.Where(p => !onlyUnknown || p.LastCommit == null).AsAsyncEnumerable();
        await foreach (var project in projects)
        {
            project.LastCommit = await _hgService.GetLastCommitTimeFromHg(project.Code, project.MigrationStatus);
        }
        await _lexBoxDbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("updateProjectType/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AdminRequired]
    public async Task<ActionResult<Project>> UpdateProjectType(Guid id)
    {
        var project = await _lexBoxDbContext.Projects.FindAsync(id);
        if (project is null) return NotFound();
        if (project.Type == ProjectType.Unknown)
        {
            project.Type = await _hgService.DetermineProjectType(project.Code, project.MigrationStatus);
            await _lexBoxDbContext.SaveChangesAsync();
        }
        return project;
    }

    [HttpGet("determineProjectType/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AdminRequired]
    public async Task<ActionResult<ProjectType>> DetermineProjectType(Guid id)
    {
        var project = await _lexBoxDbContext.Projects.FindAsync(id);
        if (project is null) return NotFound();
        return await _hgService.DetermineProjectType(project.Code, project.MigrationStatus);
    }

    [HttpPost("updateProjectTypesForUnknownProjects")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AdminRequired]
    public async Task<ActionResult<Dictionary<string, ProjectType>>> UpdateProjectTypesForUnknownProjects(int limit = 50, int offset = 0)
    {
        var projects = _lexBoxDbContext.Projects
            .Where(p => p.Type == ProjectType.Unknown && p.MigrationStatus == ProjectMigrationStatus.Migrated)
            .OrderBy(p => p.Code)
            .Skip(offset)
            .Take(limit)
            .AsAsyncEnumerable();
        var result = new Dictionary<string, ProjectType>();
        await foreach (var project in projects)
        {
            project.Type = await _hgService.DetermineProjectType(project.Code, project.MigrationStatus);
            result.Add(project.Code, project.Type);
        }
        await _lexBoxDbContext.SaveChangesAsync();
        return result;
    }

    [HttpGet("backupProject/{code}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    [HttpPost("finishResetProject/{code}")]
    [AdminRequired]
    public async Task<ActionResult> FinishResetProject(string code)
    {
        await _projectService.FinishReset(code);
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

    [HttpGet("awaitMigrated")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<bool>> AwaitMigrated(string projectCode)
    {
        if (!await _permissionService.CanAccessProject(projectCode))
            return Unauthorized();

        var token = CancellationTokenSource.CreateLinkedTokenSource(
            new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token,
            HttpContext.RequestAborted
        ).Token;
        var result = await _projectService.AwaitMigration(projectCode, token);
        if (result is null) return NotFound();
        return result.Value;
    }

    public record HgVerifyResult(string Response);
    [HttpGet("hgVerify/{code}")]
    [AdminRequired]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<HgVerifyResult>> HgVerify(string code)
    {
        var migrationStatus = await _lexBoxDbContext.Projects.Where(p => p.Code == code).Select(p => p.MigrationStatus)
            .FirstOrDefaultAsync();
        if (migrationStatus is not ProjectMigrationStatus.Migrated) return NotFound();
        var result = await _hgService.VerifyRepo(code);
        return new HgVerifyResult(result);
    }
}
