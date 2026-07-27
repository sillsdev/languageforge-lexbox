namespace FwLiteProjectSync;

/// <summary>
/// Implemented by the dry-run api wrappers so the sync service can pull out the recorded "what would
/// change" list regardless of which wrapper (record-only <see cref="DryRunMiniLcmApi"/> or record-and-apply
/// <see cref="RecordingMiniLcmApi"/>) was used.
/// </summary>
public interface IDryRunRecorder
{
    List<DryRunMiniLcmApi.DryRunRecord> DryRunRecords { get; }
}
