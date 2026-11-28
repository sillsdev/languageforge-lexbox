using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreatePartOfSpeechChange : CreateChange<PartOfSpeech>, ISelfNamedType<CreatePartOfSpeechChange>
{
    [SetsRequiredMembers]
    public CreatePartOfSpeechChange(PartOfSpeech partOfSpeech) : base(partOfSpeech.Id)
    {
        Name = partOfSpeech.Name;
        Abbreviation = partOfSpeech.Abbreviation;
        Description = partOfSpeech.Description;
        Predefined = partOfSpeech.Predefined;
    }

    // must use the name `entityId` to support json deserialization as it must match the name of the property
    [JsonConstructor]
    [SetsRequiredMembers]
    private CreatePartOfSpeechChange(Guid entityId) : base(entityId)
    {
        Name = new MultiString();
    }

    public required MultiString Name { get; init; }
    public bool Predefined { get; init; }
    public MultiString Abbreviation { get; init; } = new();
    public RichMultiString Description { get; init; } = new();

    public override ValueTask<PartOfSpeech> NewEntity(Commit commit, IChangeContext context)
    {
        return ValueTask.FromResult(new PartOfSpeech
        {
            Id = EntityId,
            Name = Name,
            Abbreviation = Abbreviation,
            Description = Description,
            Predefined = Predefined
        });
    }
}
