using Crdt.Core;
using CrdtLib.Db;
using CrdtLib.Entities;

namespace CrdtSample.Models;

public class Word : IObjectBase<Word>
{
    public required string Text { get; set; }
    public string? Note { get; set; }

    public Guid Id { get; init; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? AntonymId { get; set; }

    public Guid[] GetReferences()
    {
        return AntonymId is null ? [] : [AntonymId.Value];
    }

    public void RemoveReference(Guid id, Commit commit)
    {
        if (AntonymId == id) AntonymId = null;
    }

    public IObjectBase Copy()
    {
        return new Word
        {
            Id = Id,
            Text = Text,
            Note = Note,
            AntonymId = AntonymId,
            DeletedAt = DeletedAt
        };
    }
}