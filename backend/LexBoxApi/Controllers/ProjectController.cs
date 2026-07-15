using System.Net;
using System.Text.RegularExpressions;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Controllers.ActionResults;
using LexBoxApi.Jobs;
using LexBoxApi.Models.Project;
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
    ISchedulerFactory scheduler,
    FwHeadlessClient fwHeadlessClient,
    ILogger<ProjectController> logger)
    : ControllerBase
{
    /// <summary>
    /// Admin-only: create a new FLEx project whose repo is populated with a template .fwdata
    /// (from the SIL.LCModel package) configured for the requested writing systems.
    /// </summary>
    /// <param name="code">Project code for the new project.</param>
    /// <param name="wsVernacular">Vernacular writing system id(s); at least one is required. Repeat the query param for multiple.</param>
    /// <param name="wsAnalysis">Analysis writing system id(s); defaults to ["en"] when none are given. Repeat the query param for multiple.</param>
    /// <param name="name">Optional display name; defaults to the code.</param>
    /// <param name="anthropologyCategories">Which anthropology categories to populate (default none).</param>
    [HttpPost("createFromTemplate")]
    [AdminRequired]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Guid>> CreateFromTemplate(
        string code,
        [FromQuery] string[] wsVernacular,
        [FromQuery] string[]? wsAnalysis = null,
        string wsUi = "en",
        string? name = null,
        AnthropologyCategories anthropologyCategories = AnthropologyCategories.None,
        CancellationToken cancellationToken = default)
    {
        if (wsVernacular is null || wsVernacular.Length == 0)
            return Problem("At least one vernacular writing system is required", statusCode: StatusCodes.Status400BadRequest);
        if (code is null || code.Length < 4 || !Regex.IsMatch(code, Project.ProjectCodeRegex))
            return Problem($"Invalid project code '{code}'", statusCode: StatusCodes.Status400BadRequest);
        if (await projectService.ProjectExists(code))
            return Problem($"A project with code '{code}' already exists", statusCode: StatusCodes.Status409Conflict);

        var wsAnalysisOrDefault = AnalysisWritingSystemsOrDefault(wsAnalysis);

        var projectId = await projectService.CreateProject(new CreateProjectInput(
            Id: null,
            Name: string.IsNullOrWhiteSpace(name) ? code : name,
            Description: string.Empty,
            Code: code,
            Type: ProjectType.FLEx,
            RetentionPolicy: RetentionPolicy.Verified,
            IsConfidential: false,
            ProjectManagerId: null,
            OrgId: null));

        // Saga: the project row + empty repo now exist. Have FwHeadless populate the repo with the
        // template .fwdata; if that fails, compensate by tearing the project back down. Cleanup runs
        // on a fresh token so a client disconnect can't skip it.
        (HttpStatusCode statusCode, string? error) result;
        try
        {
            result = await fwHeadlessClient.CreateProjectFromTemplate(projectId, wsVernacular, wsAnalysisOrDefault, wsUi, anthropologyCategories, cancellationToken);
        }
        catch
        {
            await CleanupFailedCreation(projectId, code);
            throw;
        }
        if (result.error is not null)
        {
            await CleanupFailedCreation(projectId, code);
            // Surface a bad request from FwHeadless (e.g. an invalid writing-system tag) as 400, not 500.
            var statusCode = result.statusCode == HttpStatusCode.BadRequest
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status500InternalServerError;
            return Problem($"Failed to create the project from the template: {result.error}", statusCode: statusCode);
        }

        await projectService.UpdateLastCommit(code);
        return projectId;
    }

    /// <summary>Analysis writing systems to use, defaulting to English when none are supplied.</summary>
    public static string[] AnalysisWritingSystemsOrDefault(string[]? wsAnalysis) =>
        wsAnalysis is { Length: > 0 } ? wsAnalysis : ["en"];

    private async Task CleanupFailedCreation(Guid projectId, string code)
    {
        try
        {
            await projectService.CleanupFailedProjectCreation(projectId, code);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to roll back project {ProjectId} ({Code}) after a failed template creation", projectId, code);
        }
    }

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

    [HttpGet("countProjectMatches")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [AdminRequired]
    public async Task<ActionResult<Dictionary<string, int>>> CountProjectMatches(
        CancellationToken token,
        [FromQuery] ProjectType projectType,
        [FromQuery] string includeFileRegex,
        [FromQuery] string matchCountRegex,
        [FromQuery] string? excludeFileRegex)
    {
        var projectCodes = await lexBoxDbContext.Projects
            .Where(p => p.Type == projectType)
            .OrderBy(p => p.Code)
            .Select(p => p.Code)
            .ToArrayAsync();
        Dictionary<string, int> result = [];
        foreach (var projectCode in projectCodes)
        {
            try
            {
                var regexCount = await hgService.CountProjectMatches(projectCode, includeFileRegex, matchCountRegex, excludeFileRegex, token);
                result.Add(projectCode, regexCount ?? 0);
            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    return BadRequest(e.Message);
                }
                // It's possible a project was deleted in between building the code list and running the command; just skip the missing one
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    continue;
                }
                throw;
            }
        }
        return Ok(result);
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
}
