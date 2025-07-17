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

    [HttpPost("cancel/{projectId}")]
    [RequireScope(LexboxAuthScope.SendAndReceive, exclusive: false)]
    public async Task<ActionResult> CancelSync(Guid projectId)
    {
        if (!await permissionService.CanSyncProject(projectId)) return Forbid();
        var cancelled = await fwHeadlessClient.CancelCrdtSync(projectId);
        if (!cancelled) return Problem("Failed to cancel CRDT sync");
        return Ok();
    }

    [HttpGet("await-sync-finished/{projectId}")]
    [RequireScope(LexboxAuthScope.SendAndReceive, exclusive: false)]
    public async Task<ActionResult<SyncJobResult>> AwaitSyncFinished(Guid projectId)
    {
        await permissionService.AssertCanSyncProject(projectId);
        return await fwHeadlessClient.AwaitStatus(projectId, HttpContext.RequestAborted);
    }
}
