namespace MiniLcm.Models;

public record Entry : IObjectWithId<Entry>
{
    public Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public virtual MultiString LexemeForm { get; set; } = new();

    public virtual MultiString CitationForm { get; set; } = new();

    public virtual MultiString LiteralMeaning { get; set; } = new();
    public virtual List<Sense> Senses { get; set; } = [];

    public virtual MultiString Note { get; set; } = new();

    /// <summary>
    /// Components making up this complex entry
    /// </summary>
    public virtual IList<ComplexFormComponent> Components { get; set; } = [];

    /// <summary>
    /// This entry is a part of these complex forms
    /// </summary>
    public virtual IList<ComplexFormComponent> ComplexForms { get; set; } = [];

    public virtual IList<ComplexFormType> ComplexFormTypes { get; set; } = [];

    public string Headword()
    {
        //order by code to ensure the headword is stable
        //todo choose ws by preference based on ws order/default
        var word = CitationForm.Values.OrderBy(kvp => kvp.Key.Code).FirstOrDefault().Value;
        if (string.IsNullOrEmpty(word)) word = LexemeForm.Values.OrderBy(kvp => kvp.Key.Code).FirstOrDefault().Value;
        return word?.Trim() ?? "(Unknown)";
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
            Senses = [..Senses.Select(s => (Sense)s.Copy())],
            Components =
            [
                ..Components.Select(c => (ComplexFormComponent)c.Copy())
            ],
            ComplexForms =
            [
                ..ComplexForms.Select(c => (ComplexFormComponent)c.Copy())
            ],
            ComplexFormTypes =
            [
                ..ComplexFormTypes.Select(cft => (ComplexFormType)cft.Copy())
            ]
        };
    }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }

    public Entry WithoutEntryRefs()
    {
        return this with { Components = [], ComplexForms = [] };
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
