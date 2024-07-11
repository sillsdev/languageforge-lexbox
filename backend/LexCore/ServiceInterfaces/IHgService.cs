using LexCore.Entities;

namespace LexCore.ServiceInterfaces;

public record BackupExecutor(Func<Stream, CancellationToken, Task> ExecuteBackup);
public interface IHgService
{
    Task InitRepo(ProjectCode code);
    Task<DateTimeOffset?> GetLastCommitTimeFromHg(ProjectCode projectCode);
    Task<Changeset[]> GetChangesets(ProjectCode projectCode);
    Task<ProjectType> DetermineProjectType(ProjectCode projectCode);
    Task DeleteRepo(ProjectCode code);
    Task SoftDeleteRepo(ProjectCode code, string deletedRepoSuffix);
    Task<HttpContent> GetWsTagsFromFlexProject(ProjectCode code, CancellationToken token = default);
    BackupExecutor? BackupRepo(ProjectCode code);
    Task ResetRepo(ProjectCode code);
    Task FinishReset(ProjectCode code, Stream zipFile);
    Task<HttpContent> VerifyRepo(ProjectCode code, CancellationToken token);
    Task<string> GetTipHash(ProjectCode code, CancellationToken token = default);
    Task<int?> GetLexEntryCount(ProjectCode code, ProjectType projectType);
    Task<string?> GetRepositoryIdentifier(Project project);
    Task<HttpContent> ExecuteHgRecover(ProjectCode code, CancellationToken token);
    Task<HttpContent> InvalidateDirCache(ProjectCode code, CancellationToken token = default);
    bool HasAbandonedTransactions(ProjectCode projectCode);
    Task<string> HgCommandHealth();
}
