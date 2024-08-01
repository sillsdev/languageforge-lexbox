using System.Collections.Generic;
using System.Data.Common;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Controllers.ActionResults;
using LexBoxApi.Jobs;
using LexBoxApi.Services;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
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

    public record ApproveProjectJoinRequestResult(string ProjectCode, string UserName);
    [HttpPost("approveProjectJoinRequest/{projectCode}/{userId}")]
    public async Task<ApproveProjectJoinRequestResult> ApproveProjectJoinRequest(string projectCode,
        Guid userId)
    {
        await permissionService.AssertCanManageProject(projectCode);
        var projectId = await projectService.LookupProjectId(projectCode);
        var user = await lexBoxDbContext.Users.FindAsync(userId);
        // userId has already been verified when email was sent out
        lexBoxDbContext.ProjectUsers
            .Add(new ProjectUsers { ProjectId = projectId, UserId = userId, Role = ProjectRole.Editor });
        try
        {
            await lexBoxDbContext.SaveChangesAsync();
        }
        catch (DbException e) when (e.SqlState == "23505")
        {
            // Duplicate key just means someone else added the user at the same time; no problem
        }
        catch (DbUpdateException e)
        {
            var sqlState = (e.InnerException as DbException)?.SqlState;
            if (sqlState is "23505") {
                // Duplicate key just means someone else added the user at the same time; no problem
            } else {
                throw;
            }
        }
        return new(projectCode, user?.Name ?? userId.ToString());
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
    public async Task<ActionResult> ResetProject(string code)
    {
        await projectService.ResetProject(new Models.Project.ResetProjectByAdminInput(code));
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
    [ProducesDefaultResponseType]
    [AdminRequired]
    public async Task<ActionResult<Project>> DeleteProject(Guid id)
    {
        //this project is only for testing purposes. It will delete projects permanently from the database.
        var project = await lexBoxDbContext.Projects.FindAsync(id);
        if (project is null) return NotFound();
        if (project.RetentionPolicy != RetentionPolicy.Dev) return Forbid();
        lexBoxDbContext.Projects.Remove(project);
        var hgService = HttpContext.RequestServices.GetRequiredService<IHgService>();
        await hgService.DeleteRepo(project.Code);
        project.UpdateUpdatedDate();
        await lexBoxDbContext.SaveChangesAsync();
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
