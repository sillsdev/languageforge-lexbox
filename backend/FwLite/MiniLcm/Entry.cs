namespace MiniLcm;

public class Entry : IObjectWithId
{
    public virtual Guid Id { get; set; }

    public virtual MultiString LexemeForm { get; set; } = new();

    public virtual MultiString CitationForm { get; set; } = new();

    public virtual MultiString LiteralMeaning { get; set; } = new();
    public virtual IList<Sense> Senses { get; set; } = [];

    public virtual MultiString Note { get; set; } = new();
    public virtual ComplexForm? ComplexForm { get; set; }
    public virtual Variants? Variants { get; set; }

    public bool MatchesQuery(string query) =>
        LexemeForm.SearchValue(query)
        || CitationForm.SearchValue(query)
        || LiteralMeaning.SearchValue(query);

    public string Headword()
    {
        var word = CitationForm.Values.Values.FirstOrDefault();
        if (string.IsNullOrEmpty(word)) word = LexemeForm.Values.Values.FirstOrDefault();
        return word?.Trim() ?? "(Unknown)";
    }
}

public class EntryReference
{
    public required Guid EntryId { get; set; }
    public Guid? SenseId { get; set; } = null;
    public required string Headword { get; set; }
}

public class ComplexForm
{
    public Guid Id { get; set; }
    public IList<EntryReference> Components { get; set; } = [];
    public IList<ComplexFormType> Types { get; set; } = [];
}

public class Variants
{
    public Guid Id { get; set; }
    public IList<EntryReference> VariantsOf { get; set; } = [];
    public IList<VariantType> Types { get; set; } = [];
}

public class ComplexFormType
{
    public required Guid Id { get; set; }
    public required MultiString Name { get; set; }
}

public class VariantType
{
    public required Guid Id { get; set; }
    public required MultiString Name { get; set; }
}
