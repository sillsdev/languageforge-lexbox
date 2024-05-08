using System.Linq.Expressions;
using Crdt;
using Crdt.Entities;
using LinqToDB;
using MiniLcm;

namespace LcmCrdt.Objects;

public class Entry : MiniLcm.Entry, IObjectBase<Entry>, INewableObject<Entry>
{
    public static Entry New(Guid id, Commit commit)
    {
        return new()
        {
            Id = id
        };
    }

    Guid IObjectBase.Id
    {
        get => Id;
        init => Id = value;
    }

    public DateTimeOffset? DeletedAt { get; set; }


    [ExpressionMethod(nameof(HeadwordExpression))]
    public string Headword(WritingSystemId ws)
    {
        var word = CitationForm[ws];
        if (string.IsNullOrEmpty(word)) word = LexemeForm[ws];
        return word;
    }

    protected static Expression<Func<Entry, WritingSystemId, string?>> HeadwordExpression() =>
        (e, ws) => Json.Value(e.CitationForm, ms => ms[ws]) ?? Json.Value(e.LexemeForm, ms => ms[ws]);

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, Commit commit)
    {
    }

    public IObjectBase Copy()
    {
        return new Entry
        {
            Id = Id,
            DeletedAt = DeletedAt,
            LexemeForm = LexemeForm.Copy(),
            CitationForm = CitationForm.Copy(),
            LiteralMeaning = LiteralMeaning.Copy(),
            Note = Note.Copy()
        };
    }
}
