namespace MiniLcm.Models;

public record Entry : IObjectWithId<Entry>
{
    public Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public virtual MultiString LexemeForm { get; set; } = new();

    public virtual MultiString CitationForm { get; set; } = new();

    public virtual RichMultiString LiteralMeaning { get; set; } = new();
    public virtual MorphType MorphType { get; set; } = MorphType.Stem;
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

    public virtual List<Publication> PublishIn { get; set; } = [];

    /// <summary>
    /// Pre-computed headwords for all writing systems, with morph tokens applied.
    /// Populated by the backend during entry loading — not persisted in the DB.
    /// For each WS: CitationForm[ws] if present, otherwise LeadingToken + LexemeForm[ws] + TrailingToken.
    /// </summary>
    public MultiString Headword { get; set; } = new();

    public const string UnknownHeadword = "(Unknown)";

    /// <summary>
    /// Convenience method returning the first non-empty headword value (for logging, error messages, etc.).
    /// Prefers the pre-computed Headword property; falls back to CitationForm/LexemeForm if Headword is empty.
    /// </summary>
    public string HeadwordText()
    {
        var hw = Headword.Values.OrderBy(kvp => kvp.Key.Code).FirstOrDefault().Value;
        if (!string.IsNullOrEmpty(hw)) return hw.Trim();

        var word = CitationForm.Values.OrderBy(kvp => kvp.Key.Code).FirstOrDefault().Value;
        if (string.IsNullOrEmpty(word)) word = LexemeForm.Values.OrderBy(kvp => kvp.Key.Code).FirstOrDefault().Value;
        return word?.Trim() ?? UnknownHeadword;
    }

    public Entry Copy()
    {
        return new Entry
        {
            Id = Id,
            DeletedAt = DeletedAt,
            Headword = Headword.Copy(),
            LexemeForm = LexemeForm.Copy(),
            CitationForm = CitationForm.Copy(),
            LiteralMeaning = LiteralMeaning.Copy(),
            Note = Note.Copy(),
            MorphType = MorphType,
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
            PublishIn = [ ..PublishIn.Select(p => (Publication)p.Copy())]
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

public class Variants
{
    public Guid Id { get; set; }
    public IList<ComplexFormComponent> VariantsOf { get; set; } = [];
    public IList<VariantType> Types { get; set; } = [];
}


public class VariantType
{
    public required Guid Id { get; set; }
    public required MultiString Name { get; set; }
}
