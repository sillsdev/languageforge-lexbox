using System.Linq.Expressions;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using SystemTextJsonPatch;

namespace MiniLcm;

public interface IMiniLcmWriteApi
{
    Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem);

    Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        UpdateObjectInput<WritingSystem> update);
    Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after);
    // Note there's no Task DeleteWritingSystem(Guid id) because deleting writing systems needs careful consideration, as it can cause a massive cascade of data deletion

    #region PartOfSpeech
    Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech);
    Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update);
    Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after);
    Task DeletePartOfSpeech(Guid id);
    #endregion

    #region SemanticDomain
    Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain);
    Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update);
    Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after);
    Task DeleteSemanticDomain(Guid id);
    #endregion

    Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType);
    Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update);
    Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after);
    Task DeleteComplexFormType(Guid id);

    #region Entry
    Task<Entry> CreateEntry(Entry entry);
    Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update);

    Task<Entry> UpdateEntry(Entry before, Entry after);
    Task DeleteEntry(Guid id);
    Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent);
    Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent);
    Task AddComplexFormType(Guid entryId, Guid complexFormTypeId);
    Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId);
    #endregion

    #region Sense
    Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? position = null);
    Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update);
    Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after);
    Task MoveSense(Guid entryId, Guid senseId, BetweenPosition position);
    Task DeleteSense(Guid entryId, Guid senseId);
    Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain);
    Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId);
    #endregion

    #region ExampleSentence
    Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence);
    Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update);
    Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after);

    Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId);
    #endregion
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
