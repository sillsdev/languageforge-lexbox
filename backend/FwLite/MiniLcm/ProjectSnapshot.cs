using System.Text.Json.Serialization;
using MiniLcm.Models;

namespace MiniLcm;

public record ProjectSnapshot(
    Entry[] Entries,
    PartOfSpeech[] PartsOfSpeech,
    Publication[] Publications,
    SemanticDomain[] SemanticDomains,
    ComplexFormType[] ComplexFormTypes,
    MorphType[] MorphTypes,
    WritingSystems WritingSystems)
{
    public static ProjectSnapshot Empty { get; } = new([], [], [], [], [], [], new WritingSystems());

    /// <summary>
    /// Where this snapshot's contents came from. Only the persisted merge base carries it; snapshots read from
    /// fwdata leave it null, as do merge bases written before we started recording it.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SnapshotProvenance? Provenance { get; init; }
}

/// <param name="CrdtCommitId">
/// CRDT head at the moment the snapshot's contents were read, so the snapshot says which state it describes.
/// This is what makes "the merge base is stale" a testable claim instead of a guess from file dates and
/// entity counts. Null if the head could not be read.
/// </param>
public record SnapshotProvenance(Guid? CrdtCommitId, DateTimeOffset TakenAt);
