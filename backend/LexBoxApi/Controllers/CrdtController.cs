using SIL.Harmony.Core;
using LexBoxApi.Auth.Attributes;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/crdt")]
[AdminRequired]
public class CrdtController(LexBoxDbContext dbContext): ControllerBase
{
    [HttpGet("{projectId}/get")]
    public async Task<ActionResult<SyncState>> GetSyncState(Guid projectId)
    {
        return await dbContext.Set<ServerCommit>().Where(c => c.ProjectId == projectId).GetSyncState();
    }

    [HttpPost("{projectId}/add")]
    public async Task<ActionResult> Add(Guid projectId, [FromBody] ServerCommit[] commits)
    {
        foreach (var commit in commits)
        {
            commit.ProjectId = projectId;
            dbContext.Add(commit); //todo should only add if not exists, based on commit id
        }

        await dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{projectId}/changes")]
    public async Task<ActionResult<ChangesResult<ServerCommit>>> Changes(Guid projectId, [FromBody] SyncState clientHeads)
    {
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
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Code == code);
        if (project == null)
        {
            return NotFound();
        }
        return Ok(project.Id);
    }
}
