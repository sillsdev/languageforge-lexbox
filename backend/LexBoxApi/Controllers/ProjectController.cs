using LexBoxApi.Auth.Attributes;
using LexBoxApi.Controllers.ActionResults;
using LexBoxApi.Jobs;
using LexBoxApi.Services;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Path = System.IO.Path;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/project")]
public class ProjectController(
    ProjectService projectService,
    IHgService hgService,
    LexBoxDbContext lexBoxDbContext,
    IPermissionService permissionService,
    ISchedulerFactory scheduler)
    : ControllerBase
{
    [HttpPost("refreshProjectLastChanged")]
    public async Task<ActionResult> RefreshProjectLastChanged(string projectCode)
    {
        await projectService.UpdateLastCommit(projectCode);
        return Ok();
    }


    [HttpGet("lastCommitForRepo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<DateTimeOffset?>> LastCommitForRepo(string code)
    {
        var exists = await lexBoxDbContext.Projects.Where(p => p.Code == code).AnyAsync();
        if (!exists) return NotFound();
        return await hgService.GetLastCommitTimeFromHg(code);
    }

    [HttpPost("updateAllRepoCommitDates")]
    [AdminRequired]
    public async Task<ActionResult> UpdateAllRepoCommitDates(bool onlyUnknown)
    {
        var projects = lexBoxDbContext.Projects.Where(p => !onlyUnknown || p.LastCommit == null).AsAsyncEnumerable();
        await foreach (var project in projects)
        {
            project.LastCommit = await hgService.GetLastCommitTimeFromHg(project.Code);
        }

        await lexBoxDbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("updateProjectType/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AdminRequired]
    public async Task<ActionResult<Project>> UpdateProjectType(Guid id)
    {
        var project = await lexBoxDbContext.Projects.FindAsync(id);
        if (project is null) return NotFound();
        if (project.Type == ProjectType.Unknown)
        {
            project.Type = await hgService.DetermineProjectType(project.Code);
            await lexBoxDbContext.SaveChangesAsync();
        }

        return project;
    }

    [HttpPost("setProjectType")]
    [AdminRequired]
    public async Task<ActionResult> SetProjectType(string projectCode,
        ProjectType projectType,
        bool overrideKnown = false)
    {
        await lexBoxDbContext.Projects
            .Where(p => p.Code == projectCode && (p.Type == ProjectType.Unknown || overrideKnown))
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.Type, projectType));
        return Ok();
    }

    [HttpGet("projectCodeAvailable/{code}")]
    public async Task<bool> ProjectCodeAvailable(string code)
    {
        permissionService.AssertHasProjectCreatePermission();
        return !await projectService.ProjectExists(code);
    }

    [HttpGet("determineProjectType/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AdminRequired]
    public async Task<ActionResult<ProjectType>> DetermineProjectType(Guid id)
    {
        var project = await lexBoxDbContext.Projects.FindAsync(id);
        if (project is null) return NotFound();
        return await hgService.DetermineProjectType(project.Code);
    }

    [HttpPost("updateProjectTypesForUnknownProjects")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AdminRequired]
    public async Task<ActionResult<Dictionary<string, ProjectType>>> UpdateProjectTypesForUnknownProjects(int limit =
            50,
        int offset = 0)
    {
        var projects = lexBoxDbContext.Projects
            .Where(p => p.Type == ProjectType.Unknown)
            .OrderBy(p => p.Code)
            .Skip(offset)
            .Take(limit)
            .AsAsyncEnumerable();
        var result = new Dictionary<string, ProjectType>();
        await foreach (var project in projects)
        {
            project.Type = await hgService.DetermineProjectType(project.Code);
            result.Add(project.Code, project.Type);
        }

        await lexBoxDbContext.SaveChangesAsync();
        return result;
    }

    [HttpGet("backupProject/{code}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AdminRequired]
    public async Task<IActionResult> BackupProject(string code)
    {
        var backupExecutor = await projectService.BackupProject(code);
        if (backupExecutor is null)
            return NotFound();
        return new FileCallbackResult("application/zip",
            async (stream, context) => await backupExecutor.ExecuteBackup(stream, context.HttpContext.RequestAborted))
        {
            FileDownloadName = $"{code}_backup.zip"
        };
    }

    [HttpPost("resetProject/{code}")]
    [AdminRequired]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> ResetProject(string code)
    {
        try
        {
            await projectService.ResetProject(new Models.Project.ResetProjectByAdminInput(code));
        }
        catch (ProjectSyncInProgressException ex)
        {
            return Conflict(ex.Message);
        }
        return Ok();
    }

    [HttpPost("finishResetProject/{code}")]
    [AdminRequired]
    public async Task<ActionResult> FinishResetProject(string code)
    {
        await projectService.FinishReset(code);
        return Ok();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesDefaultResponseType]
    [AdminRequired]
    public async Task<ActionResult<Project>> DeleteProject(Guid id)
    {
        //this endpoint is only for testing purposes. It will delete projects permanently from the database.
        var project = await lexBoxDbContext.Projects.FindAsync(id);
        if (project is null) return NotFound();
        if (project.RetentionPolicy != RetentionPolicy.Dev) return Forbid();
        try
        {
            project = await projectService.DeleteProjectPermanently(id);
        }
        catch (ProjectSyncInProgressException ex)
        {
            return Conflict(ex.Message);
        }
        if (project is null) return NotFound();
        return project;
    }

    public record HgCommandResponse(string Response);

    [HttpGet("hgVerify/{code}")]
    [AdminRequired]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task HgVerify(string code)
    {
        var exists = await lexBoxDbContext.Projects.Where(p => p.Code == code).AnyAsync();
        if (!exists)
        {
            // Used to return NotFound() but now we have to write the response manually
            Response.StatusCode = 404;
            await Response.CompleteAsync();
            return;
        }

        var result = await hgService.VerifyRepo(code, HttpContext.RequestAborted);
        await StreamHttpResponse(result);
    }

    [HttpGet("hgRecover/{code}")]
    [AdminRequired]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task HgRecover(string code)
    {
        var exists = await lexBoxDbContext.Projects.Where(p => p.Code == code).AnyAsync();
        if (!exists)
        {
            // Used to return NotFound() but now we have to write the response manually
            Response.StatusCode = 404;
            await Response.CompleteAsync();
            return;
        }

        var result = await hgService.ExecuteHgRecover(code, HttpContext.RequestAborted);
        await StreamHttpResponse(result);
    }

    private async Task StreamHttpResponse(HttpContent hgResult)
    {
        var writer = Response.BodyWriter;
        //  Browsers want to see a content type or they won't stream the output of the fetch() call
        Response.ContentType = "text/plain; charset=utf-8";
        await hgResult.CopyToAsync(writer.AsStream());
    }

    [HttpGet("sldr-export")]
    [AdminRequired]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetLdmlZip(CancellationToken token)
    {
        var path = await projectService.PrepareLdmlZip(scheduler, token);
        if (path is null) return NotFound("No SLDR files available");
        var filename = Path.GetFileName(path);
        var stream = System.IO.File.OpenRead(path);
        return File(stream, "application/zip", filename);
    }

    [HttpPost("updateMissingLanguageList")]
    public async Task<ActionResult<string[]>> UpdateMissingLanguageList(int limit = 10)
    {
        var projects = lexBoxDbContext.Projects
            .Include(p => p.FlexProjectMetadata)
            .Where(p => p.Type == ProjectType.FLEx && p.LastCommit != null && p.FlexProjectMetadata!.WritingSystems == null)
            .Take(limit)
            .AsAsyncEnumerable();
        var codes = new List<string>(limit);
        await foreach (var project in projects)
        {
            codes.Add(project.Code);
            project.FlexProjectMetadata ??= new FlexProjectMetadata();
            project.FlexProjectMetadata.WritingSystems = await hgService.GetProjectWritingSystems(project.Code);
        }

        await lexBoxDbContext.SaveChangesAsync();

        return Ok(codes);
    }

    [HttpPost("updateMissingLangProjectId")]
    public async Task<ActionResult<string[]>> UpdateMissingLangProjectId(int limit = 10)
    {
        var projects = lexBoxDbContext.Projects
            .Include(p => p.FlexProjectMetadata)
            .Where(p => p.Type == ProjectType.FLEx && p.LastCommit != null && p.FlexProjectMetadata!.LangProjectId == null)
            .Take(limit)
            .AsAsyncEnumerable();
        var codes = new List<string>(limit);
        await foreach (var project in projects)
        {
            codes.Add(project.Code);
            project.FlexProjectMetadata ??= new FlexProjectMetadata();
            project.FlexProjectMetadata.LangProjectId = await hgService.GetProjectIdOfFlexProject(project.Code);
        }

        await lexBoxDbContext.SaveChangesAsync();

        return Ok(codes);
    }

    [HttpPost("queueUpdateProjectMetadataTask")]
    public async Task<ActionResult> QueueUpdateProjectMetadataTask(string projectCode)
    {
        await UpdateProjectMetadataJob.Queue(scheduler, projectCode);
        return Ok();
    }

    [HttpPost("block")]
    [AdminRequired]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> BlockProject(Guid? projectId = null, string? projectCode = null, string? reason = null)
    {
        var fwHeadlessClient = HttpContext.RequestServices.GetRequiredService<FwHeadlessClient>();
        try
        {
            await fwHeadlessClient.BlockProject(projectId, projectCode, reason);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("unblock")]
    [AdminRequired]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UnblockProject(Guid? projectId = null, string? projectCode = null)
    {
        var fwHeadlessClient = HttpContext.RequestServices.GetRequiredService<FwHeadlessClient>();
        try
        {
            await fwHeadlessClient.UnblockProject(projectId, projectCode);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("block-status")]
    [AdminRequired]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LexCore.Sync.SyncBlockStatus>> GetBlockStatus(Guid? projectId = null, string? projectCode = null)
    {
        var fwHeadlessClient = HttpContext.RequestServices.GetRequiredService<FwHeadlessClient>();
        var status = await fwHeadlessClient.GetBlockStatus(projectId, projectCode);
        if (status is null)
            return NotFound();
        return status;
    }
}
