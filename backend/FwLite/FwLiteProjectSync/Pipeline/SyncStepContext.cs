using MiniLcm;

namespace FwLiteProjectSync.Pipeline;

/// <summary>
/// The state handed to each <see cref="SyncStep"/>: the two apis being synced, the last-synced
/// <see cref="ProjectSnapshot"/>, running change counters, and the ability to add follow-on steps.
/// </summary>
public sealed class SyncStepContext(
    IMiniLcmApi crdtApi,
    IMiniLcmApi fwdataApi,
    ProjectSnapshot snapshot,
    Action<SyncStep> addStep)
{
    public IMiniLcmApi CrdtApi { get; } = crdtApi;
    public IMiniLcmApi FwdataApi { get; } = fwdataApi;
    public ProjectSnapshot Snapshot { get; } = snapshot;

    public int CrdtChanges { get; private set; }
    public int FwdataChanges { get; private set; }

    public void RecordCrdtChanges(int count) => CrdtChanges += count;
    public void RecordFwdataChanges(int count) => FwdataChanges += count;

    /// <summary>Add a step to the pipeline while executing. It is scheduled after its ordering constraints are met.</summary>
    public void AddStep(SyncStep step) => addStep(step);
}
