﻿using System.Linq.Expressions;
using System.Text.Json.Serialization;
using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm;

public interface IMiniLcmApi
{
    Task<WritingSystems> GetWritingSystems();
    Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem);
    Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        UpdateObjectInput<WritingSystem> update);

    IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech();
    Task CreatePartOfSpeech(PartOfSpeech partOfSpeech);
    IAsyncEnumerable<SemanticDomain> GetSemanticDomains();

    Task CreateSemanticDomain(SemanticDomain semanticDomain);

    IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null);
    IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null);
    Task<Entry?> GetEntry(Guid id);

    Task<Entry> CreateEntry(Entry entry);
    Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update);
    Task DeleteEntry(Guid id);

    Task<Sense> CreateSense(Guid entryId, Sense sense);
    Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update);
    Task DeleteSense(Guid entryId, Guid senseId);

    Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence);

    Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update);

    Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId);

    UpdateBuilder<T> CreateUpdateBuilder<T>() where T : class;
}

public record QueryOptions(SortOptions? Order = null, ExemplarOptions? Exemplar = null, int Count = 1000, int Offset = 0)
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

public interface UpdateObjectInput<T> where T : class
{
    void Apply(T obj);
    [JsonIgnore]
    JsonPatchDocument<T> Patch { get; }
}

public class UpdateBuilder<T> where T : class
{
    private readonly JsonPatchDocument<T> _patchDocument = new();

    public UpdateObjectInput<T> Build()
    {
        return new JsonPatchUpdateInput<T>(_patchDocument);
    }

    public UpdateBuilder<T> Set<T_Val>(Expression<Func<T, T_Val>> field, T_Val value)
    {
        _patchDocument.Replace(field, value);
        return this;
    }
    public UpdateBuilder<T> Add<T_Val>(Expression<Func<T, IList<T_Val>>> field, T_Val value)
    {
        _patchDocument.Add(field, value);
        return this;
    }

    /// <summary>
    /// Removes an item by index, should not be used with CRDTs.
    /// </summary>
    public UpdateBuilder<T> Remove<T_Val>(Expression<Func<T, IList<T_Val>>> field, int index)
    {
        _patchDocument.Remove(field, index);
        return this;
    }
}
