using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MiniLcm.Attributes;

namespace MiniLcm.Models;

public record Entry : IObjectWithId<Entry>
{
    public Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public virtual MultiString LexemeForm { get; set; } = new();

    public virtual MultiString CitationForm { get; set; } = new();

    public virtual RichMultiString LiteralMeaning { get; set; } = new();
    public virtual MorphTypeKind MorphType { get; set; } = MorphTypeKind.Stem;
    public virtual int HomographNumber { get; set; }
    public virtual List<Sense> Senses { get; set; } = [];

    public virtual RichMultiString Note { get; set; } = new();

    /// <summary>
    /// Components making up this complex entry
    /// </summary>
    public virtual List<ComplexFormComponent> Components { get; set; } = [];

    /// <summary>
    /// This entry is a part of these complex forms
    /// </summary>
    public virtual List<ComplexFormComponent> ComplexForms { get; set; } = [];

    public virtual List<ComplexFormType> ComplexFormTypes { get; set; } = [];

    /// <summary>
    /// This entry is a variant of these entries/senses
    /// </summary>
    public virtual List<Variant> VariantOf { get; set; } = [];

    /// <summary>
    /// Entries which are variants of this entry (or one of its senses)
    /// </summary>
    public virtual List<Variant> Variants { get; set; } = [];

    public virtual List<Publication> PublishIn { get; set; } = [];

    //Server-side query rewrite target — LcmCrdt rewrites this to Json.Query(PublishIn) so
    //filter projections (e.g. PublishInRows.Select(...).Any(...)) translate to json_each() SQL.
    [MiniLcmInternal, NotMapped, JsonIgnore, EditorBrowsable(EditorBrowsableState.Never)]
    public IEnumerable<Publication> PublishInRows => PublishIn;

    public const string UnknownHeadword = "(Unknown)";

    public string Headword()
    {
        //order by code to ensure the headword is stable
        //todo choose ws by preference based on ws order/default
        //todo this does not apply morph tokens — see #1284
        //https://github.com/sillsdev/languageforge-lexbox/issues/1284
        var word = CitationForm.Values.OrderBy(kvp => kvp.Key.Code).FirstOrDefault().Value?.Trim();
        if (string.IsNullOrEmpty(word)) word = LexemeForm.Values.OrderBy(kvp => kvp.Key.Code).FirstOrDefault().Value?.Trim();
        return string.IsNullOrEmpty(word) ? UnknownHeadword : word;
    }

    public Entry Copy()
    {
        return new Entry
        {
            Id = Id,
            DeletedAt = DeletedAt,
            LexemeForm = LexemeForm.Copy(),
            CitationForm = CitationForm.Copy(),
            LiteralMeaning = LiteralMeaning.Copy(),
            Note = Note.Copy(),
            MorphType = MorphType,
            HomographNumber = HomographNumber,
            Senses = [..Senses.Select(s => s.Copy())],
            Components =
            [
                ..Components.Select(c => c.Copy())
            ],
            ComplexForms =
            [
                ..ComplexForms.Select(c => c.Copy())
            ],
            ComplexFormTypes =
            [
                ..ComplexFormTypes.Select(cft => cft.Copy())
            ],
            VariantOf =
            [
                ..VariantOf.Select(v => v.Copy())
            ],
            Variants =
            [
                ..Variants.Select(v => v.Copy())
            ],
            PublishIn = [ ..PublishIn.Select(p => p.Copy())]
        };
    }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }
}
