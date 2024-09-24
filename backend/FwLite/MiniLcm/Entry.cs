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
    public IList<EntryReference> Components { get; set; } = [];
    public IList<ComplexFormType> Types { get; set; } = [];
    public Guid Id { get; set; }
}

public class ComplexFormType
{
    public required Guid Id { get; set; }
    public required MultiString Name { get; set; }
}
