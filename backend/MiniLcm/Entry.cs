namespace MiniLcm;

public class Entry
{
    public virtual Guid Id { get; set; }

    public virtual MultiString LexemeForm { get; set; } = new();

    public virtual MultiString CitationForm { get; set; } = new();

    public virtual MultiString LiteralMeaning { get; set; } = new();
    public virtual IList<Sense> Senses { get; set; } = [];

    public virtual MultiString Note { get; set; } = new();

    public bool MatchesQuery(string query) =>
        LexemeForm.SearchValue(query)
        || CitationForm.SearchValue(query)
        || LiteralMeaning.SearchValue(query);
}
