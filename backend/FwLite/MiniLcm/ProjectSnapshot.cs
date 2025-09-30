using MiniLcm.Models;

namespace MiniLcm;

public record ProjectSnapshot(
    Entry[] Entries,
    PartOfSpeech[] PartsOfSpeech,
    Publication[] Publications,
    SemanticDomain[] SemanticDomains,
    ComplexFormType[] ComplexFormTypes,
    WritingSystems WritingSystems)
{
    public static ProjectSnapshot Empty { get; } = new([], [], [], [], [], new WritingSystems());
}
