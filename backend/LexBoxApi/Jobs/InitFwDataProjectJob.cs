using System.Net;
using System.Text.Json;
using LexBoxApi.Services;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexCore.Sync;
using LexData;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

namespace LexBoxApi.Jobs;

/// <summary>
/// Has FwHeadless populate the empty repo of a project created by ProjectController.InitFwDataProject,
/// then marks the project ready, or rolls it back if FwHeadless reports that the creation failed.
/// </summary>
/// <remarks>
/// Quartz keeps this job in the database and reruns it if LexBox stops partway through (RequestRecovery),
/// so every run must be safe to repeat. Each run therefore starts by finding out how far an earlier run
/// got, using LexBox's own repo as the source of truth. When the outcome is unknown (FwHeadless is
/// unreachable, busy, or still running a creation), the job schedules another attempt rather than guess.
/// </remarks>
public class InitFwDataProjectJob(
    LexBoxDbContext dbContext,
    ProjectService projectService,
    FwHeadlessClient fwHeadlessClient,
    IHgService hgService,
    ISchedulerFactory schedulerFactory,
    IMemoryCache memoryCache,
    ILogger<InitFwDataProjectJob> logger) : LexJob
{
    public const int MaxAttempts = 10;
    public static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    public static JobKey Key { get; } = new("InitFwDataProjectJob", "ProjectCreation");

    public static async Task Queue(ISchedulerFactory schedulerFactory,
        Guid projectId,
        InitFwDataProjectInput input,
        int attempt = 1,
        TimeSpan? delay = null,
        CancellationToken cancellationToken = default)
    {
        await QueueJob(schedulerFactory,
            Key,
            new JobDataMap
            {
                { nameof(ProjectId), projectId.ToString() },
                // The job store only takes strings, so the writing systems travel as JSON
                { nameof(Input), JsonSerializer.Serialize(input) },
                { nameof(Attempt), attempt.ToString() },
            },
            delay,
            cancellationToken);
    }

    public string? ProjectId { get; set; }
    public string? Input { get; set; }
    public string? Attempt { get; set; }

    protected override async Task ExecuteJob(IJobExecutionContext context)
    {
        ArgumentException.ThrowIfNullOrEmpty(ProjectId);
        ArgumentException.ThrowIfNullOrEmpty(Input);
        ArgumentException.ThrowIfNullOrEmpty(Attempt);
        if (!Guid.TryParse(ProjectId, out var projectId)) throw new ArgumentException($"Invalid project ID {ProjectId}", nameof(ProjectId));
        if (!int.TryParse(Attempt, out var attempt)) throw new ArgumentException($"Invalid number {Attempt}", nameof(Attempt));
        var input = JsonSerializer.Deserialize<InitFwDataProjectInput>(Input)
            ?? throw new ArgumentException("Missing creation input", nameof(Input));

        var project = await dbContext.Projects.FindAsync(projectId);
        if (project is null)
        {
            logger.LogInformation("Project {ProjectId} no longer exists, nothing to initialise", projectId);
            return;
        }
        if (project.FwDataInitStatus != FwDataInitStatus.InProgress)
        {
            // An earlier run finished the job and this is a duplicate (e.g. a recovered run)
            logger.LogInformation("Project {ProjectId} ({Code}) has already been initialised", projectId, project.Code);
            return;
        }

        var outcome = await AttemptInit(fwHeadlessClient, hgService, logger, projectId, project.Code, input);
        switch (outcome.Result)
        {
            case InitAttemptResult.Created:
                await projectService.FinishFwDataInit(projectId);
                logger.LogInformation("Initialised project {ProjectId} ({Code}) from the template", projectId, project.Code);
                return;

            case InitAttemptResult.Failed:
                logger.LogError("Creating project {ProjectId} ({Code}) from the template failed, rolling it back: {Error}",
                    projectId, project.Code, outcome.Detail);
                // Before the rollback, so a poller that finds the project gone always finds the reason too
                RecordFailure(memoryCache, projectId, outcome.Detail);
                try
                {
                    await projectService.CleanupFailedProjectCreation(projectId, project.Code);
                    return;
                }
                catch (Exception ex)
                {
                    // The project is still marked as being initialised, so a later attempt starts over: it
                    // either creates the project after all, or fails again and retries the rollback
                    logger.LogError(ex, "Failed to roll back project {ProjectId} ({Code})", projectId, project.Code);
                }
                break;

            case InitAttemptResult.RetryLater:
                logger.LogWarning("Couldn't tell whether project {ProjectId} ({Code}) was created (attempt {Attempt} of {MaxAttempts}): {Reason}",
                    projectId, project.Code, attempt, MaxAttempts, outcome.Detail);
                break;
        }

        if (attempt >= MaxAttempts)
        {
            logger.LogError(
                "Giving up on initialising project {ProjectId} ({Code}) after {Attempts} attempts; it is still marked as being initialised and needs manual cleanup",
                projectId, project.Code, attempt);
            return;
        }
        await Queue(schedulerFactory, projectId, input, attempt + 1, RetryDelay);
    }

    /// <summary>
    /// One attempt at getting the project populated, picking up wherever an earlier attempt got to.
    /// Never throws: anything unexpected means the outcome is unknown, so it's reported as RetryLater.
    /// </summary>
    /// <remarks>
    /// None of these calls take a cancellation token, for the same reason ProjectController used not to
    /// pass one: FwHeadless finishes a creation whatever happens to our side of the connection, so
    /// abandoning the wait would only stop us learning the outcome. If LexBox shuts down mid-attempt,
    /// Quartz recovers the job and the next run picks the creation up again.
    /// </remarks>
    public static async Task<InitAttemptOutcome> AttemptInit(FwHeadlessClient fwHeadlessClient,
        IHgService hgService,
        ILogger logger,
        Guid projectId,
        string code,
        InitFwDataProjectInput input)
    {
        try
        {
            // FwHeadless's push is the last step of a creation, so a non-empty repo means an earlier attempt
            // succeeded, even if we never heard back. Trust this over FwHeadless's own status, which says
            // Created as soon as the .fwdata exists on its disk, even if FwHeadless died before pushing it.
            if (await RepoHasCommits(hgService, code)) return InitAttemptOutcome.Created;

            var status = await fwHeadlessClient.CreationStatus(projectId);
            if (status?.Status == ProjectCreationStatusEnum.Creating)
            {
                // An earlier attempt is still running in FwHeadless (e.g. this job was interrupted and
                // recovered); starting another would be refused, so wait for that one instead
                status = await fwHeadlessClient.AwaitCreationFinished(projectId);
                if (await RepoHasCommits(hgService, code)) return InitAttemptOutcome.Created;
                return status?.Status == ProjectCreationStatusEnum.CreationFailed
                    ? InitAttemptOutcome.Failed(status.Error ?? "FwHeadless reported that the creation failed")
                    : InitAttemptOutcome.RetryLater($"Waiting for FwHeadless's creation ended with {status?.Status.ToString() ?? "no status"}");
            }

            // Clear anything an interrupted attempt left in FwHeadless's local project folder, which would
            // otherwise break the new attempt. Throws ProjectSyncInProgressException if FwHeadless is busy
            // with this project.
            await fwHeadlessClient.DeleteProject(projectId);
            var (statusCode, error) = await fwHeadlessClient.InitFwDataProject(projectId, input.WsVernacular, input.WsAnalysis, input.WsUi);
            if (error is null) return InitAttemptOutcome.Created;
            // FwHeadless refused to start because something else holds the project, so nothing was built
            if (statusCode == HttpStatusCode.Conflict) return InitAttemptOutcome.RetryLater(error);
            // FwHeadless answered with a failure, so the creation isn't running and FwHeadless has already
            // cleaned up its own local state
            return InitAttemptOutcome.Failed(error);
        }
        catch (Exception ex)
        {
            // Most likely FwHeadless is unreachable (e.g. restarting) or our request timed out. FwHeadless
            // may still be creating the project, so only a later attempt can tell how this ended.
            logger.LogWarning(ex, "Attempt to initialise project {ProjectId} ({Code}) ended without an answer", projectId, code);
            return InitAttemptOutcome.RetryLater(ex.Message);
        }
    }

    private static async Task<bool> RepoHasCommits(IHgService hgService, string code) =>
        await hgService.GetTipHash(code) != HgService.AllZeroHash;

    // In memory only: once the rollback has deleted the project row, this is all that's left to tell a
    // poller why. It doesn't survive a LexBox restart, after which the poller sees "not found".
    private static string FailureCacheKey(Guid projectId) => $"InitFwDataProjectFailure|{projectId}";

    private static void RecordFailure(IMemoryCache memoryCache, Guid projectId, string? error) =>
        memoryCache.Set(FailureCacheKey(projectId), error ?? "Creating the project from the template failed", TimeSpan.FromHours(1));

    public static string? TryGetRecentFailure(IMemoryCache memoryCache, Guid projectId) =>
        memoryCache.Get<string>(FailureCacheKey(projectId));
}

public enum InitAttemptResult
{
    Created,
    // FwHeadless definitely isn't creating the project and didn't create it
    Failed,
    // The outcome is unknown, e.g. FwHeadless was unreachable or is still busy
    RetryLater,
}

public record InitAttemptOutcome(InitAttemptResult Result, string? Detail = null)
{
    public static InitAttemptOutcome Created { get; } = new(InitAttemptResult.Created);
    public static InitAttemptOutcome Failed(string error) => new(InitAttemptResult.Failed, error);
    public static InitAttemptOutcome RetryLater(string reason) => new(InitAttemptResult.RetryLater, reason);
}
