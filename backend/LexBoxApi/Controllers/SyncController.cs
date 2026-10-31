using LexBoxApi.Auth.Attributes;
using LexBoxApi.Services;
using LexCore.Auth;
using LexCore.ServiceInterfaces;
using LexCore.Sync;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/fw-lite/sync")]
[ApiExplorerSettings(GroupName = LexboxOpenApi.OpenApiPublicDocumentName)]
public class SyncController(
    IPermissionService permissionService,
    FwHeadlessClient fwHeadlessClient,
    ProjectService projectService,
    CrdtCommitService crdtCommitService) : ControllerBase
{
    [HttpGet("status/{projectId}")]
    [RequireScope(LexboxAuthScope.SendAndReceive)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProjectSyncStatus>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectSyncStatus>> GetSyncStatus(Guid projectId)
    {
        if (!await permissionService.CanViewProject(projectId)) return Forbid();
        return Ok(await fwHeadlessClient.CrdtSyncStatus(projectId));
    }

    [HttpPost("trigger/{projectId}")]
    [RequireScope(LexboxAuthScope.SendAndReceive, exclusive: false)]
    public async Task<ActionResult> TriggerSync(Guid projectId)
    {
        if (!await permissionService.CanSyncProject(projectId)) return Forbid();
        await fwHeadlessClient.SyncMercurialAndHarmony(projectId);
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
        await fwHeadlessClient.SyncHarmony(projectId);
        return Ok();
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
        await fwHeadlessClient.RegenerateProjectSnapshot(projectId, commitId, preserveAllFieldWorksCommits);
        return Ok();
    }

    /// <summary>
    /// Adds a commit that makes each client of a project rebuild its CRDT snapshots the next time it syncs.
    /// Nothing is rebuilt here, and a client that never syncs never rebuilds. A rebuild replays the whole
    /// history, which is slow on a large project.
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <param name="note">Recorded in the commit's metadata, e.g. why the rebuild was needed.</param>
    [HttpPost("request-crdt-snapshot-rebuild/{projectId}")]
    [AdminRequired]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<SnapshotRebuildCommit>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SnapshotRebuildCommit>> RequestCrdtSnapshotRebuild(Guid projectId, [FromQuery][BindRequired] string note)
    {
        if (string.IsNullOrWhiteSpace(note)) return BadRequest("A note is required");
        var rebuild = await crdtCommitService.AddSnapshotRebuildCommit(projectId, note);
        if (rebuild is null) return NotFound("Project has no CRDT commits");
        return rebuild;
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

        await fwHeadlessClient.BlockProject(id.Value, reason);
        return Ok();
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

        await fwHeadlessClient.UnblockProject(id.Value);
        return Ok();
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

        return await fwHeadlessClient.GetBlockStatus(id.Value);
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
