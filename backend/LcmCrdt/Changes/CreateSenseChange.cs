using System.Text.Json.Serialization;
using CrdtLib.Changes;
using CrdtLib.Db;
using CrdtLib.Entities;
using MiniLcm;

namespace LcmCrdt.Changes;

public class CreateSenseChange: Change<Sense>, ISelfNamedType<CreateSenseChange>
{
    public CreateSenseChange(MiniLcm.Sense sense, Guid entryId) : base(sense.Id == Guid.Empty ? Guid.NewGuid() : sense.Id)
    {
        sense.Id = EntityId;
        EntryId = entryId;
        Definition = sense.Definition;
        SemanticDomain = sense.SemanticDomain;
        Gloss = sense.Gloss;
        PartOfSpeech = sense.PartOfSpeech;
    }

    [JsonConstructor]
    private CreateSenseChange(Guid entityId, Guid entryId) : base(entityId)
    {
        EntryId = entryId;
    }

    public Guid EntryId { get; set; }
    public MultiString? Definition { get; set; }
    public MultiString? Gloss { get; set; }
    public string? PartOfSpeech { get; set; }
    public IList<string>? SemanticDomain { get; set; }

    public override IObjectBase NewEntity(Commit commit)
    {
        return new Sense
        {
            Id = EntityId,
            EntryId = EntryId,
            Definition = Definition ?? new MultiString(),
            Gloss = Gloss ?? new MultiString(),
            PartOfSpeech = PartOfSpeech ?? string.Empty,
            SemanticDomain = SemanticDomain ?? []
        };
    }

    public override async ValueTask ApplyChange(Sense entity, ChangeContext context)
    {
        if (Definition is not null) entity.Definition = Definition;
        if (Gloss is not null) entity.Gloss = Gloss;
        if (PartOfSpeech is not null) entity.PartOfSpeech = PartOfSpeech;
        if (SemanticDomain is not null) entity.SemanticDomain = SemanticDomain;
        if (await context.IsObjectDeleted(EntryId))
        {
            entity.DeletedAt = context.Commit.DateTime;
        }
    }
}
