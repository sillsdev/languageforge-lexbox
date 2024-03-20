using System.Linq.Expressions;
using System.Text.Json.Serialization;
using SystemTextJsonPatch;

namespace MiniLcm;

public interface ILexboxApi
{
    Task<WritingSystems> GetWritingSystems();
    // Task<string[]> GetExemplars();
    // Task<Entry[]> GetEntries(string exemplar, QueryOptions? options = null);
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

public record QueryOptions(string Order, int Count = 1000, int Offset = 0)
{
    public static QueryOptions Default { get; } = new QueryOptions("");
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
}
