namespace LcmCrdt;

/// <summary>
/// Told when opening a project rebuilds all of its snapshots, so the host can show the user what is going on.
/// Why that happens at open time: <see cref="CurrentProjectService"/>.
/// </summary>
public interface ISnapshotRebuildListener
{
    void RebuildStarted(CrdtProject project);
    void RebuildFinished(CrdtProject project);
}
