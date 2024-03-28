using LexCore.Entities;

namespace LexCore.ServiceInterfaces;

public record BackupExecutor(Func<Stream, CancellationToken, Task> ExecuteBackup);
public interface IHgService
{
    Task InitRepo(string code);
    Task<DateTimeOffset?> GetLastCommitTimeFromHg(string projectCode);
    Task<Changeset[]> GetChangesets(string projectCode);
    Task<ProjectType> DetermineProjectType(string projectCode);
    Task DeleteRepo(string code);
    Task SoftDeleteRepo(string code, string deletedRepoSuffix);
    BackupExecutor? BackupRepo(string code);
    Task ResetRepo(string code);
    Task FinishReset(string code, Stream zipFile);
    Task<HttpContent> VerifyRepo(string code, CancellationToken token);
    Task<int?> GetLexEntryCount(string code);
    Task<string?> GetRepositoryIdentifier(Project project);
    Task<HttpContent> ExecuteHgRecover(string code, CancellationToken token);
    bool HasAbandonedTransactions(string projectCode);
    Task<string> HgCommandHealth();
}
