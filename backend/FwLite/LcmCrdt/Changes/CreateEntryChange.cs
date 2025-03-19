﻿using System.Text.Json.Serialization;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;


public class CreateEntryChange : CreateChange<Entry>, ISelfNamedType<CreateEntryChange>
{
    public CreateEntryChange(Entry entry) : base(entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id)
    {
        entry.Id = EntityId;
        LexemeForm = entry.LexemeForm;
        CitationForm = entry.CitationForm;
        LiteralMeaning = entry.LiteralMeaning;
        Note = entry.Note;
    }

    [JsonConstructor]
    private CreateEntryChange(Guid entityId) : base(entityId)
    {
    }

    public MultiString? LexemeForm { get; set; }

    public MultiString? CitationForm { get; set; }

    public RichMultiString? LiteralMeaning { get; set; }

    public RichMultiString? Note { get; set; }

    public override ValueTask<Entry> NewEntity(Commit commit, IChangeContext context)
    {
        return new(new Entry
        {
            Id = EntityId,
            LexemeForm = LexemeForm ?? new MultiString(),
            CitationForm = CitationForm ?? new MultiString(),
            LiteralMeaning = LiteralMeaning ?? new(),
            Note = Note ?? new()
        });
    }
}
