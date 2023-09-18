using LexCore.Entities;

namespace LexCore.ServiceInterfaces;

public interface IHgService
{
    Task InitRepo(string code);
    Task<DateTimeOffset?> GetLastCommitTimeFromHg(string projectCode, ProjectMigrationStatus migrationStatus);
    Task<Changeset[]> GetChangesets(string projectCode, ProjectMigrationStatus migrationStatus);
    Task DeleteRepo(string code);
    Task SoftDeleteRepo(string code, string deletedRepoSuffix);
    Task<string?> BackupRepo(string code);
    Task ResetRepo(string code);
}
