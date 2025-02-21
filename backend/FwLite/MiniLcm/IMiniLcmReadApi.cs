using System.Text.Json.Serialization;
using MiniLcm.Models;

namespace MiniLcm;

public interface IMiniLcmReadApi
{
    Task<WritingSystems> GetWritingSystems();
    IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech();
    IAsyncEnumerable<Publication> GetPublications();
    IAsyncEnumerable<SemanticDomain> GetSemanticDomains();
    IAsyncEnumerable<ComplexFormType> GetComplexFormTypes();
    Task<ComplexFormType?> GetComplexFormType(Guid id);
    IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null);
    IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null);
    Task<Entry?> GetEntry(Guid id);
    Task<Sense?> GetSense(Guid entryId, Guid id);
    Task<PartOfSpeech?> GetPartOfSpeech(Guid id);
    Task<Publication?> GetPublication(Guid id);
    Task<SemanticDomain?> GetSemanticDomain(Guid id);
    Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id);
}

public record QueryOptions(
    SortOptions? Order = null,
    ExemplarOptions? Exemplar = null,
    int Count = 1000,
    int Offset = 0)
{
    public static QueryOptions Default { get; } = new();
    public const int QueryAll = -1;
    public SortOptions Order { get; init; } = Order ?? SortOptions.Default;

    public IEnumerable<T> ApplyPaging<T>(IEnumerable<T> enumerable)
    {
        if (Offset > 0)
            enumerable = enumerable.Skip(Offset);
        if (Count == QueryAll) return enumerable;
        return enumerable.Take(Count);
    }

    public IQueryable<T> ApplyPaging<T>(IQueryable<T> queryable)
    {
        if (Offset > 0)
            queryable = queryable.Skip(Offset);
        if (Count == QueryAll) return queryable;
        return queryable.Take(Count);
    }
}

public record SortOptions(SortField Field, WritingSystemId WritingSystem, bool Ascending = true)
{
    public static SortOptions Default { get; } = new(SortField.Headword, "default");
}

public record ExemplarOptions(string Value, WritingSystemId WritingSystem);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortField
{
    Headword, //citation form -> lexeme form
}
