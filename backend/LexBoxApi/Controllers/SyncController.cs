using LexBoxApi.Auth.Attributes;
using LexBoxApi.Services;
using LexCore;
using LexCore.Auth;
using LexCore.ServiceInterfaces;
using LexCore.Sync;
using Microsoft.AspNetCore.Mvc;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/fw-lite/sync")]
[ApiExplorerSettings(GroupName = LexBoxKernel.OpenApiPublicDocumentName)]
public class SyncController(
    IPermissionService permissionService,
    FwHeadlessClient fwHeadlessClient) : ControllerBase
{
    [HttpGet("status/{projectId}")]
    [RequireScope(LexboxAuthScope.SendAndReceive)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseTypeAttribute<ProjectSyncStatus>(StatusCodes.Status200OK)]
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
        var started = await fwHeadlessClient.CrdtSync(projectId);
        if (!started) return Problem("Failed to sync CRDT");
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
        catch (OperationCanceledException e)
        {
            return Ok(new SyncJobResult(SyncJobStatusEnum.TimedOutAwaitingSyncStatus, "Timed out awaiting sync status"));
        }
    }

    [HttpPost("regenerate-snapshot/{projectId}")]
    [RequireScope(LexboxAuthScope.SendAndReceive, exclusive: false)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RegenerateProjectSnapshot(Guid projectId)
    {
        await permissionService.AssertCanSyncProject(projectId);
        try
        {
            var result = await fwHeadlessClient.RegenerateProjectSnapshot(projectId);
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
}
