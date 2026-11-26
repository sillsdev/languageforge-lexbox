
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

    public virtual List<Publication> PublishIn { get; set; } = [];

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

    /// <summary>
    /// Sorts alphabetically, like FieldWorks. One complex form can use this entry and several of its senses,
    /// so ties go to the whole entry first, then the senses in order. Call after <see cref="Senses"/> is sorted.
    /// </summary>
    public void SortComplexForms(IComparer<string> headwordComparer)
    {
        if (ComplexForms.Count < 2) return;
        ComplexForms.Sort((a, b) =>
        {
            var result = headwordComparer.Compare(a.ComplexFormHeadword ?? string.Empty, b.ComplexFormHeadword ?? string.Empty);
            if (result != 0) return result;
            result = a.ComplexFormEntryId.CompareTo(b.ComplexFormEntryId);
            if (result != 0) return result;
            result = SensePosition(a.ComponentSenseId).CompareTo(SensePosition(b.ComponentSenseId));
            if (result != 0) return result;
            return Nullable.Compare(a.ComponentSenseId, b.ComponentSenseId);
        });
    }

    private int SensePosition(Guid? senseId)
    {
        if (senseId is null) return -1;
        var index = Senses.FindIndex(s => s.Id == senseId);
        return index < 0 ? int.MaxValue : index;
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
