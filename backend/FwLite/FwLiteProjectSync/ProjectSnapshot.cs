using MiniLcm.Models;

namespace FwLiteProjectSync;

public record ProjectSnapshot(
    Entry[] Entries,
    PartOfSpeech[] PartsOfSpeech,
    Publication[] Publications,
    SemanticDomain[] SemanticDomains,
    ComplexFormType[] ComplexFormTypes,
    WritingSystems WritingSystems)
{
    internal static ProjectSnapshot Empty { get; } = new([], [], [], [], [], new WritingSystems());
}
