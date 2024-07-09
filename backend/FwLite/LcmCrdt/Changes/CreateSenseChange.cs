using System.Text.Json.Serialization;
using Crdt;
using Crdt.Changes;
using Crdt.Db;
using Crdt.Entities;
using MiniLcm;

namespace LcmCrdt.Changes;

public class CreateSenseChange: CreateChange<Sense>, ISelfNamedType<CreateSenseChange>
{
    public CreateSenseChange(MiniLcm.Sense sense, Guid entryId) : base(sense.Id == Guid.Empty ? Guid.NewGuid() : sense.Id)
    {
        sense.Id = EntityId;
        EntryId = entryId;
        Definition = sense.Definition;
        SemanticDomains = sense.SemanticDomains;
        Gloss = sense.Gloss;
        PartOfSpeech = sense.PartOfSpeech;
        PartOfSpeechId = sense.PartOfSpeechId;
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
    public Guid? PartOfSpeechId { get; set; }
    public IList<SemanticDomain>? SemanticDomains { get; set; }

    public override async ValueTask<IObjectBase> NewEntity(Commit commit, ChangeContext context)
    {
        return new Sense
        {
            Id = EntityId,
            EntryId = EntryId,
            Definition = Definition ?? new MultiString(),
            Gloss = Gloss ?? new MultiString(),
            PartOfSpeech = PartOfSpeech ?? string.Empty,
            PartOfSpeechId = PartOfSpeechId,
            SemanticDomains = SemanticDomains ?? [],
            DeletedAt = await context.IsObjectDeleted(EntryId) ? commit.DateTime : (DateTime?)null
        };
    }
}
