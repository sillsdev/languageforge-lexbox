namespace MiniLcm.Models;

public class Entry : IObjectWithId
{
    public Guid Id { get; set; }

    public virtual MultiString LexemeForm { get; set; } = new();

    public virtual MultiString CitationForm { get; set; } = new();

    public virtual MultiString LiteralMeaning { get; set; } = new();
    public virtual IList<Sense> Senses { get; set; } = [];

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
        var word = CitationForm.Values.Values.FirstOrDefault();
        if (string.IsNullOrEmpty(word)) word = LexemeForm.Values.Values.FirstOrDefault();
        return word?.Trim() ?? "(Unknown)";
    }
}

public record ComplexFormComponent
{
    public static ComplexFormComponent FromEntries(Entry complexFormEntry, Entry componentEntry, Guid? componentSenseId = null)
    {
        if (componentEntry.Id == default) throw new ArgumentException("componentEntry.Id is empty");
        if (complexFormEntry.Id == default) throw new ArgumentException("complexFormEntry.Id is empty");
        return new ComplexFormComponent
        {
            Id = Guid.NewGuid(),
            ComplexFormEntryId = complexFormEntry.Id,
            ComplexFormHeadword = complexFormEntry.Headword(),
            ComponentEntryId = componentEntry.Id,
            ComponentHeadword = componentEntry.Headword(),
            ComponentSenseId = componentSenseId,
        };
    }
    public Guid Id { get; set; }
    public virtual required Guid ComplexFormEntryId { get; set; }
    public string? ComplexFormHeadword { get; set; }
    public virtual required Guid ComponentEntryId { get; set; }
    public virtual Guid? ComponentSenseId { get; set; } = null;
    public string? ComponentHeadword { get; set; }
}

public class Variants
{
    public Guid Id { get; set; }
    public IList<ComplexFormComponent> VariantsOf { get; set; } = [];
    public IList<VariantType> Types { get; set; } = [];
}

//todo support an order for the complex form types, might be here, or on the entry
public class ComplexFormType
{
    public virtual Guid Id { get; set; }
    public required MultiString Name { get; set; }
}

public class VariantType
{
    public required Guid Id { get; set; }
    public required MultiString Name { get; set; }
}
