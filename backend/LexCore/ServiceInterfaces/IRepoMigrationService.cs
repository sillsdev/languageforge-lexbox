using System.Threading.Channels;
using LexCore.Entities;

namespace LexCore.ServiceInterfaces;

public interface IRepoMigrationService
{
    ChannelReader<string> MigrationCompleted { get; }

    /// used for tests to determine that the projects have been queried from the db on startup
    Task Started { get; }

    void QueueMigration(string projectCode);

    /// <summary>
    /// Notify that a project is being sent & received. This will block migration from starting. It will return null if a migration has started indicating that send receive should be blocked
    /// </summary>
    /// <param name="projectCode"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    ValueTask<IDisposable?> BeginSendReceive(string projectCode, ProjectMigrationStatus status = ProjectMigrationStatus.PublicRedmine);

    Task WaitMigrationFinishedAsync(string projectCode, CancellationToken cancellationToken);
}
