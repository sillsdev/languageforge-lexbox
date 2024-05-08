using System.Text.Json.Serialization;
using Crdt;
using Crdt.Changes;
using Crdt.Entities;
using MiniLcm;

namespace LcmCrdt.Changes;


public class CreateEntryChange : Change<Entry>, ISelfNamedType<CreateEntryChange>
{
    public CreateEntryChange(MiniLcm.Entry entry) : base(entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id)
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

    public MultiString? LiteralMeaning { get; set; }

    public MultiString? Note { get; set; }

    public override IObjectBase NewEntity(Commit commit)
    {
        return new Entry
        {
            Id = EntityId,
            LexemeForm = LexemeForm ?? new MultiString(),
            CitationForm = CitationForm ?? new MultiString(),
            LiteralMeaning = LiteralMeaning ?? new MultiString(),
            Note = Note ?? new MultiString()
        };
    }

    public override ValueTask ApplyChange(Entry entity, ChangeContext context)
    {
        if (LexemeForm is not null) entity.LexemeForm = LexemeForm;
        if (CitationForm is not null) entity.CitationForm = CitationForm;
        if (LiteralMeaning is not null) entity.LiteralMeaning = LiteralMeaning;
        if (Note is not null) entity.Note = Note;
        return ValueTask.CompletedTask;
    }
}
