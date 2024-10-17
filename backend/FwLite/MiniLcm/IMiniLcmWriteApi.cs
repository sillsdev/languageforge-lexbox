using System.Linq.Expressions;
using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm;

public interface IMiniLcmWriteApi
{
    Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem);

    Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        UpdateObjectInput<WritingSystem> update);

    Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent);
    Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent);
    Task ReplaceComplexFormComponent(ComplexFormComponent old, ComplexFormComponent @new);

    Task CreatePartOfSpeech(PartOfSpeech partOfSpeech);
    Task CreateSemanticDomain(SemanticDomain semanticDomain);
    Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType);
    Task<Entry> CreateEntry(Entry entry);
    Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update);
    Task DeleteEntry(Guid id);
    Task<Sense> CreateSense(Guid entryId, Sense sense);
    Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update);
    Task DeleteSense(Guid entryId, Guid senseId);
    Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain);
    Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId);

    Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence);

    Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update);

    Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId);
}

/// <summary>
/// wrapper around JsonPatchDocument that allows for fluent updates
/// </summary>
/// <param name="patchDocument"></param>
/// <typeparam name="T"></typeparam>
public class UpdateObjectInput<T>(JsonPatchDocument<T> patchDocument) where T : class
{
    public UpdateObjectInput() : this(new JsonPatchDocument<T>()) { }
    public JsonPatchDocument<T> Patch { get; } = patchDocument;

    public void Apply(T obj)
    {
        Patch.ApplyTo(obj);
    }

    public UpdateObjectInput<T> Set<T_Val>(Expression<Func<T, T_Val>> field, T_Val value)
    {
        Patch.Replace(field, value);
        return this;
    }

    public UpdateObjectInput<T> Add<T_Val>(Expression<Func<T, IList<T_Val>>> field, T_Val value)
    {
        Patch.Add(field, value);
        return this;
    }

    /// <summary>
    /// Removes an item by index, should not be used with CRDTs.
    /// </summary>
    public UpdateObjectInput<T> Remove<T_Val>(Expression<Func<T, IList<T_Val>>> field, int index)
    {
        Patch.Remove(field, index);
        return this;
    }
}
