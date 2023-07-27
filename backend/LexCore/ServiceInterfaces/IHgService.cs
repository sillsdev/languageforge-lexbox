using LexCore.Entities;

namespace LexCore.ServiceInterfaces;

public interface IHgService
{
    Task InitRepo(string code);
    Task<DateTimeOffset?> GetLastCommitTimeFromHg(string projectCode);
    Task<Changeset[]> GetChangesets(string projectCode);
    Task DeleteRepo(string code);
    Task<string> BackupRepo(string code);
    Task<string> ResetRepo(string code);
}
