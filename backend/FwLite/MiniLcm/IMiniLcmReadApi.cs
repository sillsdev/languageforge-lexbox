using System.Linq.Expressions;
using System.Text;
using System.Text.Json.Serialization;
using MiniLcm.Filtering;
using MiniLcm.Media;
using MiniLcm.Models;

namespace MiniLcm;

public interface IMiniLcmReadApi
{
    Task<WritingSystems> GetWritingSystems();
    Task<WritingSystem?> GetWritingSystem(WritingSystemId id, WritingSystemType type);
    IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech();
    IAsyncEnumerable<Publication> GetPublications();
    IAsyncEnumerable<SemanticDomain> GetSemanticDomains();
    IAsyncEnumerable<ComplexFormType> GetComplexFormTypes();
    IAsyncEnumerable<MorphTypeData> GetAllMorphTypeData();
    Task<ComplexFormType?> GetComplexFormType(Guid id);
    Task<MorphTypeData?> GetMorphTypeData(Guid id);
    Task<int> CountEntries(string? query = null, FilterQueryOptions? options = null);
    IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null);
    IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null);
    Task<Entry?> GetEntry(Guid id);
    Task<Sense?> GetSense(Guid entryId, Guid id);
    Task<PartOfSpeech?> GetPartOfSpeech(Guid id);
    Task<Publication?> GetPublication(Guid id);
    Task<SemanticDomain?> GetSemanticDomain(Guid id);
    Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id);
    Task<EntryWindowResponse> GetEntriesWindow(int start, int size, string? query = null, QueryOptions? options = null);
    /// <summary>
    /// Get the index of an entry within the sorted/filtered entry list.
    /// Returns -1 if the entry is not found.
    /// </summary>
    Task<int> GetEntryIndex(Guid entryId, string? query = null, QueryOptions? options = null);

    Task<ReadFileResponse> GetFileStream(MediaUri mediaUri)
    {
        return Task.FromResult(new ReadFileResponse(ReadFileResult.NotSupported));
    }
}

public record EntryWindowResponse(
    IReadOnlyList<Entry> Entries,
    int FirstIndex)
{
}



public record FilterQueryOptions(
    ExemplarOptions? Exemplar = null,
    EntryFilter? Filter = null)
{
    public static FilterQueryOptions Default { get; } = new();
    public bool HasFilter => Filter is {GridifyFilter.Length: > 0 } || Exemplar is {Value.Length: > 0};
    public virtual FilterQueryOptions Normalized(NormalizationForm form)
    {
        return new(Exemplar?.Normalized(form), Filter?.Normalized(form));
    }
}

public record QueryOptions(
    SortOptions? Order = null,
    ExemplarOptions? Exemplar = null,
    int Count = QueryOptions.DefaultCount,
    int Offset = 0,
    EntryFilter? Filter = null) : FilterQueryOptions(Exemplar, Filter)
{
    public static new QueryOptions Default { get; } = new();
    public const int QueryAll = -1;
    public const int DefaultCount = 1000;
    public SortOptions Order { get; init; } = Order ?? SortOptions.Default;

    public override QueryOptions Normalized(NormalizationForm form)
    {
        return new(Order, Exemplar?.Normalized(form), Count, Offset, Filter?.Normalized(form));
    }

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

    public IOrderedEnumerable<T> ApplyOrder<T, TKey>(IEnumerable<T> enumerable, Func<T, TKey> orderFunc)
    {
        if (Order.Ascending)
        {
            return enumerable.OrderBy(orderFunc);
        }
        else
        {
            return enumerable.OrderByDescending(orderFunc);
        }
    }

    public IOrderedQueryable<T> ApplyOrder<T, TKey>(IQueryable<T> enumerable, Expression<Func<T, TKey>> orderFunc)
    {
        if (Order.Ascending)
        {
            return enumerable.OrderBy(orderFunc);
        }
        else
        {
            return enumerable.OrderByDescending(orderFunc);
        }
    }
}

public record SortOptions(SortField Field, WritingSystemId WritingSystem = default, bool Ascending = true)
{
    public const string DefaultWritingSystem = "default";
    public static SortOptions Default { get; } = new(SortField.Headword, DefaultWritingSystem);
}

public record ExemplarOptions(string Value, WritingSystemId WritingSystem)
{
    public ExemplarOptions Normalized(NormalizationForm form)
    {
        return new(Value.Normalize(form), WritingSystem);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortField
{
    Headword, //citation form -> lexeme form
    SearchRelevance
}
