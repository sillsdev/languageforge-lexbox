using System.Text.Json.Serialization;

namespace MiniLcm.Models;

[JsonPolymorphic]
[JsonDerivedType(typeof(Entry), nameof(Entry))]
[JsonDerivedType(typeof(Sense), nameof(Sense))]
[JsonDerivedType(typeof(ExampleSentence), nameof(ExampleSentence))]
[JsonDerivedType(typeof(WritingSystem), nameof(WritingSystem))]
[JsonDerivedType(typeof(PartOfSpeech), nameof(PartOfSpeech))]
[JsonDerivedType(typeof(SemanticDomain), nameof(SemanticDomain))]
public interface IObjectWithId
{
    public Guid Id { get; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Guid[] GetReferences();

    public void RemoveReference(Guid id, DateTimeOffset time);

    public IObjectWithId Copy();
}
