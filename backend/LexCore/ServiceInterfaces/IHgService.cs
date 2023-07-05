using LexCore.Entities;

namespace LexCore.ServiceInterfaces;

public interface IHgService
{
    Task InitRepo(string code);
    Task<DateTimeOffset?> GetLastCommitTimeFromHg(string projectCode);
    Task<Changeset[]> GetChangesets(string projectCode);
}
