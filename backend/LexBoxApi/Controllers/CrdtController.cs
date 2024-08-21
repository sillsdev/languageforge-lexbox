using SIL.Harmony.Core;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Hub;
using LexBoxApi.Services;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MiniLcm.Push;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/crdt")]
[AdminRequired]
public class CrdtController(
    LexBoxDbContext dbContext,
    IHubContext<CrdtProjectChangeHub, IProjectChangeListener> hubContext,
    IPermissionService permissionService,
    ProjectService projectService) : ControllerBase
{
    [HttpGet("{projectId}/get")]
    public async Task<ActionResult<SyncState>> GetSyncState(Guid projectId)
    {
        await permissionService.AssertCanSyncProject(projectId);
        return await dbContext.Set<ServerCommit>().Where(c => c.ProjectId == projectId).GetSyncState();
    }

    [HttpPost("{projectId}/add")]
    public async Task<ActionResult> Add(Guid projectId, [FromBody] ServerCommit[] commits)
    {
        await permissionService.AssertCanSyncProject(projectId);
        foreach (var commit in commits)
        {
            commit.ProjectId = projectId;
            dbContext.Add(commit); //todo should only add if not exists, based on commit id
        }

        await dbContext.SaveChangesAsync();
        await hubContext.Clients.Group(CrdtProjectChangeHub.ProjectGroup(projectId)).OnProjectUpdated(projectId);
        return Ok();
    }

    [HttpPost("{projectId}/changes")]
    public async Task<ActionResult<ChangesResult<ServerCommit>>> Changes(Guid projectId,
        [FromBody] SyncState clientHeads)
    {
        await permissionService.AssertCanSyncProject(projectId);
        var commits = dbContext.Set<ServerCommit>().Where(c => c.ProjectId == projectId);
        return await commits.GetChanges<ServerCommit, ServerJsonChange>(clientHeads);
    }

    public record LexboxCrdtProject(Guid Id, string Name);

    [HttpGet("listProjects")]
    public async Task<ActionResult<LexboxCrdtProject[]>> ListProjects()
    {
        return await dbContext.Projects
            .Where(p => dbContext.Set<ServerCommit>().Any(c => c.ProjectId == p.Id))
            .Select(p => new LexboxCrdtProject(p.Id, p.Code))
            .ToArrayAsync();
    }

    [HttpGet("lookupProjectId")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<Guid>> GetProjectId(string code)
    {
        await permissionService.AssertCanViewProject(code);
        var projectId = await projectService.LookupProjectId(code);
        if (projectId == default)
        {
            return NotFound();
        }

        return Ok(projectId);
    }
}
