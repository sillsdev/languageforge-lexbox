using System.Linq.Expressions;
using System.Text.Json.Serialization;
using SIL.Harmony;
using SIL.Harmony.Entities;
using LinqToDB;
using MiniLcm.Models;

namespace LcmCrdt.Objects;

public class Entry : MiniLcm.Models.Entry, IObjectBase<Entry>
{
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
        return word.Trim();
    }

    protected static Expression<Func<Entry, WritingSystemId, string?>> HeadwordExpression() =>
        (e, ws) => (string.IsNullOrEmpty(Json.Value(e.CitationForm, ms => ms[ws])) ? Json.Value(e.LexemeForm, ms => ms[ws]) : Json.Value(e.CitationForm, ms => ms[ws]))!.Trim();

    public Guid[] GetReferences()
    {
        return
        [
            ..Components.SelectMany(c => c.ComponentSenseId is null ? [c.ComponentEntryId] : new [] {c.ComponentEntryId, c.ComponentSenseId.Value}),
            ..ComplexForms.Select(c => c.ComplexFormEntryId)
        ];
    }

    public void RemoveReference(Guid id, Commit commit)
    {
        Components = Components.Where(c => c.ComponentEntryId != id && c.ComponentSenseId != id).ToList();
        ComplexForms = ComplexForms.Where(c => c.ComplexFormEntryId != id).ToList();
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
            Note = Note.Copy(),
            Senses = [..Senses.Select(s => (s is Sense cs ? (Sense) cs.Copy() : s))],
            Components = [..Components.Select(c => (c is CrdtComplexFormComponent cc ? (ComplexFormComponent)cc.Copy() : c))],
            ComplexForms = [..ComplexForms.Select(c => (c is CrdtComplexFormComponent cc ? (ComplexFormComponent)cc.Copy() : c))],
            ComplexFormTypes = [..ComplexFormTypes.Select(cft => (cft is CrdtComplexFormType ct ? (ComplexFormType) ct.Copy() : cft))],
            Variants = Variants
        };
    }
}
