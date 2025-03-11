﻿using System.Text.Json.Serialization;
using LcmCrdt.Utils;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreateSenseChange: CreateChange<Sense>, ISelfNamedType<CreateSenseChange>
{
    public CreateSenseChange(Sense sense, Guid entryId) : base(sense.Id == Guid.Empty ? Guid.NewGuid() : sense.Id)
    {
        sense.Id = EntityId;
        EntryId = entryId;
        Order = sense.Order;
        Definition = sense.Definition;
        SemanticDomains = sense.SemanticDomains;
        Gloss = sense.Gloss;
        PartOfSpeechId = sense.PartOfSpeech?.Id ?? sense.PartOfSpeechId;
    }

    [JsonConstructor]
    private CreateSenseChange(Guid entityId, Guid entryId) : base(entityId)
    {
        EntryId = entryId;
    }

    public Guid EntryId { get; set; }
    public double Order { get; set; }
    public RichMultiString? Definition { get; set; }
    public MultiString? Gloss { get; set; }
    public Guid? PartOfSpeechId { get; set; }
    public IList<SemanticDomain>? SemanticDomains { get; set; }

    public override async ValueTask<Sense> NewEntity(Commit commit, IChangeContext context)
    {
        return new Sense
        {
            Id = EntityId,
            EntryId = EntryId,
            Order = Order,
            Definition = Definition ?? new(),
            Gloss = Gloss ?? new MultiString(),
            PartOfSpeechId = await context.DeletedAsNull(PartOfSpeechId),
            SemanticDomains = await context.FilterDeleted(SemanticDomains ?? []).ToArrayAsync(),
            DeletedAt = await context.IsObjectDeleted(EntryId) ? commit.DateTime : (DateTime?)null
        };
    }
}
