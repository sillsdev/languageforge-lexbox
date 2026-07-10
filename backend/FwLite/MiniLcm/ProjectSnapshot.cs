using MiniLcm.Models;

namespace MiniLcm;

public record ProjectSnapshot(
    Entry[] Entries,
    PartOfSpeech[] PartsOfSpeech,
    Publication[] Publications,
    SemanticDomain[] SemanticDomains,
    ComplexFormType[] ComplexFormTypes,
    MorphType[] MorphTypes,
    VariantType[] VariantTypes,
    WritingSystems WritingSystems)
{
    public static ProjectSnapshot Empty { get; } = new([], [], [], [], [], [], [], new WritingSystems());

    //Snapshots and templates serialized before variant support have no VariantTypes property, so
    //System.Text.Json passes null for the constructor parameter; this init coalesces it back to
    //empty (covered by the legacy SnapshotDeserializationRegressionData test).
    public VariantType[] VariantTypes { get; init; } = VariantTypes ?? [];
}
