using LexCore.Entities;
using System.IO.Compression;

namespace LexCore.ServiceInterfaces;

public record BackupExecutor(Func<Stream, CancellationToken, Task> ExecuteBackup);
public interface IHgService
{
    Task InitRepo(ProjectCode code);
    Task CopyRepo(ProjectCode sourceCode, ProjectCode destCode);
    Task<DateTimeOffset?> GetLastCommitTimeFromHg(ProjectCode projectCode);
    Task<Changeset[]> GetChangesets(ProjectCode projectCode);
    Task<ProjectType> DetermineProjectType(ProjectCode projectCode);
    Task DeleteRepoIfExists(ProjectCode code);
    Task SoftDeleteRepo(ProjectCode code, string deletedRepoSuffix);
    Task<ProjectWritingSystems?> GetProjectWritingSystems(ProjectCode code, CancellationToken token = default);
    Task<Guid?> GetProjectIdOfFlexProject(ProjectCode code, CancellationToken token = default);
    Task<int?> GetModelVersionOfFlexProject(ProjectCode code, CancellationToken token = default);
    BackupExecutor? BackupRepo(ProjectCode code);
    Task ResetRepo(ProjectCode code);
    Task FinishReset(ProjectCode code, Stream zipFile);
    Task<HttpContent> VerifyRepo(ProjectCode code, CancellationToken token);
    Task<string> GetTipHash(ProjectCode code, CancellationToken token = default);
    Task<int?> GetRepoSizeInKb(ProjectCode code, CancellationToken token = default);
    Task<int?> GetLexEntryCount(ProjectCode code, ProjectType projectType);
    Task<string?> GetRepositoryIdentifier(Project project);
    Task<ZipArchive?> GetLdmlZip(ProjectCode code, CancellationToken token = default);
    Task<HttpContent> ExecuteHgRecover(ProjectCode code, CancellationToken token);
    Task<HttpContent> InvalidateDirCache(ProjectCode code, CancellationToken token = default);
    bool HasAbandonedTransactions(ProjectCode projectCode);
    Task<string> HgCommandHealth();

    Task<string[]> CleanupResetBackups(bool dryRun = false);
}
