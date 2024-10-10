using System.Text.Json.Serialization;
using MiniLcm.Models;

namespace MiniLcm;

public interface IMiniLcmReadApi
{
    Task<WritingSystems> GetWritingSystems();
    IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech();
    IAsyncEnumerable<SemanticDomain> GetSemanticDomains();
    IAsyncEnumerable<ComplexFormType> GetComplexFormTypes();
    IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null);
    IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null);
    Task<Entry?> GetEntry(Guid id);
}

public record QueryOptions(
    SortOptions? Order = null,
    ExemplarOptions? Exemplar = null,
    int Count = 1000,
    int Offset = 0)
{
    public static QueryOptions Default { get; } = new();
    public SortOptions Order { get; init; } = Order ?? SortOptions.Default;
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
