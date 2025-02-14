using System.Text.Json.Serialization;
using SIL.Harmony.Core;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Hub;
using LexBoxApi.Services;
using LexCore;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexCore.Sync;
using LexData;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MiniLcm.Push;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/crdt")]
[ApiExplorerSettings(GroupName = LexBoxKernel.OpenApiPublicDocumentName)]
public class CrdtController(
    LexBoxDbContext dbContext,
    IHubContext<CrdtProjectChangeHub, IProjectChangeListener> hubContext,
    IPermissionService permissionService,
    LoggedInContext loggedInContext,
    ProjectService projectService,
    FwHeadlessClient fwHeadlessClient) : ControllerBase
{
    private DbSet<ServerCommit> ServerCommits => dbContext.Set<ServerCommit>();

    [HttpGet("{projectId}/get")]
    public async Task<ActionResult<SyncState>> GetSyncState(Guid projectId)
    {
        await permissionService.AssertCanSyncProject(projectId);
        return await ServerCommits.Where(c => c.ProjectId == projectId).GetSyncState();
    }

    [HttpPost("{projectId}/add")]
    public async Task<ActionResult> Add(Guid projectId, [FromBody] ServerCommit[] commits, Guid? clientId)
    {
        await permissionService.AssertCanSyncProject(projectId);
        foreach (var commit in commits)
        {
            commit.ProjectId = projectId;
            dbContext.Add(commit); //todo should only add if not exists, based on commit id
        }

        await dbContext.SaveChangesAsync();
        await hubContext.Clients.Group(CrdtProjectChangeHub.ProjectGroup(projectId)).OnProjectUpdated(projectId, clientId);
        return Ok();
    }

    //using an async enumerable here to reduce memory usage as it allows streaming the Commits
    public record ChangesResult(IAsyncEnumerable<ServerCommit> MissingFromClient, SyncState ServerSyncState): IChangesResult
    {
        [JsonIgnore]//just to ensure type safety
        IEnumerable<CommitBase> IChangesResult.MissingFromClient => MissingFromClient.ToBlockingEnumerable();
    }

    [HttpPost("{projectId}/changes")]
    public async Task<ActionResult<ChangesResult>> Changes(Guid projectId,
        [FromBody] SyncState clientHeads)
    {
        await permissionService.AssertCanSyncProject(projectId);
        var commits = ServerCommits.Where(c => c.ProjectId == projectId);
        var localState = await commits.GetSyncState();
        return new ChangesResult(commits.GetMissingCommits<ServerCommit, ServerJsonChange>(localState, clientHeads), localState);
    }

    public record FwLiteProject(Guid Id, string Code, string Name, bool IsFwDataProject, bool IsCrdtProject);

    [HttpGet("listProjects")]
    public async Task<ActionResult<FwLiteProject[]>> ListProjects()
    {
        return await projectService.UserProjects(loggedInContext.User.Id)
            .Where(p => p.Type == ProjectType.FLEx)
            .Select(p => new FwLiteProject(p.Id, p.Code, p.Name, p.LastCommit != null, ServerCommits.Any(c => c.ProjectId == p.Id)))
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
