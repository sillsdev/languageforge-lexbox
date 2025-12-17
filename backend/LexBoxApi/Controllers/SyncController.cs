using LexBoxApi.Auth.Attributes;
using LexBoxApi.Services;
using LexCore.Auth;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexCore.Sync;
using Microsoft.AspNetCore.Mvc;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/fw-lite/sync")]
[ApiExplorerSettings(GroupName = LexBoxKernel.OpenApiPublicDocumentName)]
public class SyncController(
    IPermissionService permissionService,
    FwHeadlessClient fwHeadlessClient,
    ProjectService projectService) : ControllerBase
{
    [HttpGet("status/{projectId}")]
    [RequireScope(LexboxAuthScope.SendAndReceive)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProjectSyncStatus>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectSyncStatus>> GetSyncStatus(Guid projectId)
    {
        if (!await permissionService.CanViewProject(projectId)) return Forbid();
        var result = await fwHeadlessClient.CrdtSyncStatus(projectId);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost("trigger/{projectId}")]
    [RequireScope(LexboxAuthScope.SendAndReceive, exclusive: false)]
    public async Task<ActionResult> TriggerSync(Guid projectId)
    {
        if (!await permissionService.CanSyncProject(projectId)) return Forbid();
        var (started, statusCode, error) = await fwHeadlessClient.SyncMercurialAndHarmony(projectId);
        if (!started)
        {
            return statusCode switch
            {
                System.Net.HttpStatusCode.Locked => Problem(error ?? "Project is blocked from syncing", statusCode: StatusCodes.Status423Locked),
                System.Net.HttpStatusCode.NotFound => Problem(error ?? "Project not found", statusCode: StatusCodes.Status404NotFound),
                _ => Problem(error ?? "Failed to trigger sync", statusCode: (int)statusCode)
            };
        }
        return Ok();
    }

    [HttpGet("await-sync-finished/{projectId}")]
    [RequireScope(LexboxAuthScope.SendAndReceive, exclusive: false)]
    public async Task<ActionResult<SyncJobResult>> AwaitSyncFinished(Guid projectId)
    {
        await permissionService.AssertCanSyncProject(projectId);
        try
        {
            return await fwHeadlessClient.AwaitStatus(projectId, HttpContext.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            return Ok(new SyncJobResult(SyncJobStatusEnum.TimedOutAwaitingSyncStatus, "Timed out awaiting sync status"));
        }
    }

    /// <summary>
    /// Syncs the fw-headless and Lexbox copies of the specified Harmony project.
    /// If a project is reset, depending on the state of the Mercurial project,
    /// doing this and then regenerating the snapshot is a possible approach to get back into a valid state.
    /// This is somewhat experimental.
    /// </summary>
    [HttpPost("sync-harmony/{projectId}")]
    [RequireScope(LexboxAuthScope.SendAndReceive, exclusive: false)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncHarmony(Guid projectId)
    {
        await permissionService.AssertCanSyncProject(projectId);
        try
        {
            var result = await fwHeadlessClient.SyncHarmony(projectId);
            if (result is not null)
            {
                return Problem(result);
            }
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.Message);
        }
    }

    /// <summary>
    /// Regenerates the project snapshot (the JSON representation of the project data used by the frontend).
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <param name="commitId">Optional. If provided, the snapshot will be regenerated based on the state of the project at this specific commit. Subsequent commits will be ignored.</param>
    /// <param name="preserveAllFieldWorksCommits">
    /// Optional. If true, FieldWorks commits after the specified commit will be preserved in the snapshot.
    /// This makes sense, because any FieldWorks commits that have made it into the FWLite project have clearly been successfully synced,
    /// which is what the snapshot is intended to represent.
    /// </param>
    [HttpPost("regenerate-snapshot/{projectId}")]
    [RequireScope(LexboxAuthScope.SendAndReceive, exclusive: false)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RegenerateProjectSnapshot(Guid projectId, [FromQuery] Guid? commitId = null, [FromQuery] bool preserveAllFieldWorksCommits = false)
    {
        await permissionService.AssertCanSyncProject(projectId);
        try
        {
            var result = await fwHeadlessClient.RegenerateProjectSnapshot(projectId, commitId, preserveAllFieldWorksCommits);
            if (result is not null)
            {
                return Problem(result);
            }
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.Message);
        }
    }

    [HttpPost("block")]
    [RequireScope(LexboxAuthScope.SendAndReceive, exclusive: false)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> BlockProject(Guid? projectId = null, string? projectCode = null, string? reason = null)
    {
        if (!IsProjectIdOrCodeProvided(projectId, projectCode))
            return BadRequest("Either projectId or projectCode must be provided");

        var id = await ResolveProjectId(projectId, projectCode);
        if (id is null)
            return NotFound($"Project code '{projectCode}' not found");

        await permissionService.AssertCanSyncProject(id.Value);

        try
        {
            await fwHeadlessClient.BlockProject(id.Value, reason);
            return Ok();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("unblock")]
    [RequireScope(LexboxAuthScope.SendAndReceive, exclusive: false)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UnblockProject(Guid? projectId = null, string? projectCode = null)
    {
        if (!IsProjectIdOrCodeProvided(projectId, projectCode))
            return BadRequest("Either projectId or projectCode must be provided");

        var id = await ResolveProjectId(projectId, projectCode);
        if (id is null)
            return NotFound($"Project code '{projectCode}' not found");

        await permissionService.AssertCanSyncProject(id.Value);

        try
        {
            await fwHeadlessClient.UnblockProject(id.Value);
            return Ok();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("block-status")]
    [RequireScope(LexboxAuthScope.SendAndReceive, exclusive: false)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SyncBlockStatus>> GetBlockStatus(Guid? projectId = null, string? projectCode = null)
    {
        if (!IsProjectIdOrCodeProvided(projectId, projectCode))
            return BadRequest("Either projectId or projectCode must be provided");

        var id = await ResolveProjectId(projectId, projectCode);
        if (id is null)
            return NotFound($"Project code '{projectCode}' not found");

        // Check permissions with the resolved projectId
        await permissionService.AssertCanViewProject(id.Value);

        var status = await fwHeadlessClient.GetBlockStatus(id.Value);
        if (status is null)
            return NotFound();
        return status;
    }

    private static bool IsProjectIdOrCodeProvided(Guid? projectId, string? projectCode)
    {
        return projectId.HasValue || !string.IsNullOrWhiteSpace(projectCode);
    }

    private async Task<Guid?> ResolveProjectId(Guid? projectId, string? projectCode)
    {
        if (projectId.HasValue)
        {
            return projectId.Value;
        }

        if (string.IsNullOrWhiteSpace(projectCode))
        {
            return null;
        }

        return await projectService.LookupProjectId(projectCode);
    }
}
