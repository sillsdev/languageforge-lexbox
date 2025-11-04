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

    public const string UnknownHeadword = "(Unknown)";

    public string Headword()
    {
        //order by code to ensure the headword is stable
        //todo choose ws by preference based on ws order/default
        //https://github.com/sillsdev/languageforge-lexbox/issues/1284
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
