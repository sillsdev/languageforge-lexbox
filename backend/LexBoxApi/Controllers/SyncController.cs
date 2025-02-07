using LexBoxApi.Services;
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
    public async Task<ActionResult<ProjectSyncStatus>> GetSyncStatus(Guid projectId)
    {
        if (!await permissionService.CanViewProject(projectId)) return Forbid();
        var result = await fwHeadlessClient.CrdtSyncStatus(projectId);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost("trigger/{projectId}")]
    public async Task<ActionResult<SyncResult>> TriggerSync(Guid projectId)
    {
        if (!await permissionService.CanSyncProject(projectId)) return Forbid();
        var result = await fwHeadlessClient.CrdtSync(projectId);
        if (result is null) return NotFound(); // TODO: Handle ProblemHttpResult? Or just let it return 404 like this?
        return Ok(result);
    }
}
