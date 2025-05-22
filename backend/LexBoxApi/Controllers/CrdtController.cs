using System.Text.Json.Serialization;
using SIL.Harmony.Core;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.GraphQL;
using LexBoxApi.Hub;
using LexBoxApi.Models;
using LexBoxApi.Services;
using LexCore.Auth;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniLcm.Push;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/crdt")]
[RequireScope(LexboxAuthScope.SendAndReceive)]
[ApiExplorerSettings(GroupName = LexBoxKernel.OpenApiPublicDocumentName)]
public class CrdtController(
    LexBoxDbContext dbContext,
    IHubContext<CrdtProjectChangeHub, IProjectChangeListener> hubContext,
    IPermissionService permissionService,
    LoggedInContext loggedInContext,
    ProjectService projectService,
    CrdtCommitService crdtCommitService,
    LexAuthService lexAuthService
    ) : ControllerBase
{
    [HttpGet("{projectId}/get")]
    public async Task<ActionResult<SyncState>> GetSyncState(Guid projectId)
    {
        await permissionService.AssertCanSyncProject(projectId);
        return await crdtCommitService.GetSyncState(projectId);
    }

    [HttpPost("{projectId}/add")]

    public async Task<ActionResult> Add(Guid projectId, StreamJsonAsyncEnumerable<ServerCommit> commits, Guid? clientId, CancellationToken token)
    {
        await permissionService.AssertCanSyncProject(projectId);
        await crdtCommitService.AddCommits(projectId, commits, token);

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
        var localState = await crdtCommitService.GetSyncState(projectId);
        return new ChangesResult(crdtCommitService.GetMissingCommits(projectId, localState, clientHeads), localState);
    }

    [HttpPost("{projectId}/countChanges")]
    public async Task<ActionResult<int>> CountChanges(Guid projectId,
        [FromBody] SyncState clientHeads)
    {
        await permissionService.AssertCanSyncProject(projectId);
        var localState = await crdtCommitService.GetSyncState(projectId);
        return await crdtCommitService.ApproximatelyCountMissingCommits(projectId, localState, clientHeads);
    }

    public record FwLiteProject(Guid Id, string Code, string Name, bool IsFwDataProject, bool IsCrdtProject);

    [HttpGet("listProjects")]
    public async Task<ActionResult<FwLiteProject[]>> ListProjects()
    {
        var myProjects = await projectService.UserProjects(loggedInContext.User.Id)
            .Where(p => p.Type == ProjectType.FLEx)
            .Select(p => new FwLiteProject(p.Id, p.Code, p.Name, p.LastCommit != null, dbContext.Set<ServerCommit>().Any(c => c.ProjectId == p.Id)))
            .ToArrayAsync();
        if (loggedInContext.User.IsOutOfSyncWithMyProjects(myProjects.Select(p => p.Id).ToArray()))
        {
            await lexAuthService.RefreshUser(LexAuthConstants.ProjectsClaimType);
        }
        return myProjects;
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

    [HttpGet("checkConnection")]
    public ActionResult CheckConnection()
    {
        return Ok();
    }
}
