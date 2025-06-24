using LinqToDB.Mapping;

namespace LcmCrdt.FullTextSearch;

public record EntrySearchRecord
{
    public Guid Id { get; init; }
    public required string Headword { get; init; }
    public required string CitationForm { get; init; }
    public required string LexemeForm { get; init; }
    public required string Gloss { get; init; }
    public required string Definition { get; init; }
}
