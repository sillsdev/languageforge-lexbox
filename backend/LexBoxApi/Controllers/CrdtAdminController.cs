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
    /// Makes every client of a project rebuild its Harmony snapshots on its next sync, by adding an empty
    /// commit dated before the project's oldest commit. Use it when a project's sync fails repeatedly with
    /// a foreign key violation from AddSnapshots (sillsdev/harmony#105).
    /// Each client replays its whole history once, which takes a while on a large project.
    /// </summary>
    /// <param name="note">added to the commit's metadata, e.g. a ticket to explain why this was needed</param>
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
