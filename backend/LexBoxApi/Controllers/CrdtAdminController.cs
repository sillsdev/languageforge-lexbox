using LexBoxApi.Auth.Attributes;
using LexBoxApi.Hub;
using LexBoxApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MiniLcm.Push;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/admin/crdt")]
[AdminRequired]
public class CrdtAdminController(
    CrdtCommitService crdtCommitService,
    IHubContext<CrdtProjectChangeHub, IProjectChangeHubClient> hubContext) : ControllerBase
{
    /// <summary>
    /// Makes every client of a project rebuild its snapshots on its next sync. Each one replays its whole
    /// history, which is slow on a large project.
    /// </summary>
    /// <param name="note">recorded in the commit's metadata, e.g. why the rebuild was needed</param>
    [HttpPost("{projectId}/forceSnapshotRebuild")]
    public async Task<ActionResult<CrdtCommitService.SnapshotRebuildCommit>> ForceSnapshotRebuild(Guid projectId,
        string? note = null)
    {
        var rebuild = await crdtCommitService.AddSnapshotRebuildCommit(projectId, note);
        if (rebuild is null) return NotFound("Project has no CRDT commits");
        await hubContext.Clients.Group(CrdtProjectChangeHub.ProjectGroup(projectId))
            .OnProjectUpdated(projectId, null);
        return rebuild;
    }
}
