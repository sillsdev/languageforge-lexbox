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

    //snapshots and templates serialized before variant support deserialize this as null
    public VariantType[] VariantTypes { get; init; } = VariantTypes ?? [];
}
