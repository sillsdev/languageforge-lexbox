using System.Text;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Wrappers;

namespace MiniLcm.Validators;

public class MiniLcmApiStringNormalizationWrapperFactory() : IMiniLcmWrapperFactory
{
    public IMiniLcmApi Create(IMiniLcmApi api, IProjectIdentifier _unused) => Create(api);

    public IMiniLcmApi Create(IMiniLcmApi api)
    {
        return new MiniLcmApiStringNormalizationWrapper(api);
    }
}

public partial class MiniLcmApiStringNormalizationWrapper(
    IMiniLcmApi api) : IMiniLcmApi
{
    public const NormalizationForm Form = NormalizationForm.FormD;

    [BeaKona.AutoInterface(IncludeBaseInterfaces = true, MemberMatch = BeaKona.MemberMatchTypes.Any)]
    private readonly IMiniLcmApi _api = api;

    // ********** Overrides go here **********

    // Read operations
    public IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null)
    {
        return _api.SearchEntries(query.Normalize(Form), options?.Normalized(Form));
    }

    public Task<int> CountEntries(string? query = null, FilterQueryOptions? options = null)
    {
        return _api.CountEntries(query?.Normalize(Form), options?.Normalized(Form));
    }

    public IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        return _api.GetEntries(options?.Normalized(Form));
    }

    // Write operations - WritingSystem
    public Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? between = null)
    {
        return _api.CreateWritingSystem(writingSystem.Normalized(), between);
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update)
    {
        return _api.UpdateWritingSystem(id, type, update.Normalized());
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api = null)
    {
        return _api.UpdateWritingSystem(before.Normalized(), after.Normalized(), api);
    }

    // Write operations - Entry
    public Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options = null)
    {
        return _api.CreateEntry(entry.Normalized(), options);
    }

    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        return _api.UpdateEntry(id, update.Normalized());
    }

    public Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api = null)
    {
        return _api.UpdateEntry(before.Normalized(), after.Normalized(), api);
    }

    // Write operations - Sense
    public Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? position = null)
    {
        return _api.CreateSense(entryId, sense.Normalized(), position);
    }

    public Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        return _api.UpdateSense(entryId, senseId, update.Normalized());
    }

    public Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null)
    {
        return _api.UpdateSense(entryId, before.Normalized(), after.Normalized(), api);
    }

    // Write operations - ExampleSentence
    public Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null)
    {
        return _api.CreateExampleSentence(entryId, senseId, exampleSentence.Normalized(), position);
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, UpdateObjectInput<ExampleSentence> update)
    {
        return _api.UpdateExampleSentence(entryId, senseId, exampleSentenceId, update.Normalized());
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, ExampleSentence before, ExampleSentence after, IMiniLcmApi? api = null)
    {
        return _api.UpdateExampleSentence(entryId, senseId, before.Normalized(), after.Normalized(), api);
    }

    // Write operations - Translation
    public Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation)
    {
        return _api.AddTranslation(entryId, senseId, exampleSentenceId, translation.Normalized());
    }

    public Task UpdateTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId, UpdateObjectInput<Translation> update)
    {
        return _api.UpdateTranslation(entryId, senseId, exampleSentenceId, translationId, update.Normalized());
    }

    // Write operations - PartOfSpeech
    public Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        return _api.CreatePartOfSpeech(partOfSpeech.Normalized());
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        return _api.UpdatePartOfSpeech(id, update.Normalized());
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api = null)
    {
        return _api.UpdatePartOfSpeech(before.Normalized(), after.Normalized(), api);
    }

    // Write operations - SemanticDomain
    public Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        return _api.CreateSemanticDomain(semanticDomain.Normalized());
    }

    public Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        return _api.UpdateSemanticDomain(id, update.Normalized());
    }

    public Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api = null)
    {
        return _api.UpdateSemanticDomain(before.Normalized(), after.Normalized(), api);
    }

    // Write operations - ComplexFormType
    public Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        return _api.CreateComplexFormType(complexFormType.Normalized());
    }

    public Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        return _api.UpdateComplexFormType(id, update.Normalized());
    }

    public Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api = null)
    {
        return _api.UpdateComplexFormType(before.Normalized(), after.Normalized(), api);
    }

    // Write operations - Publication
    public Task<Publication> CreatePublication(Publication pub)
    {
        return _api.CreatePublication(pub.Normalized());
    }

    public Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        return _api.UpdatePublication(id, update.Normalized());
    }

    public Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        return _api.UpdatePublication(before.Normalized(), after.Normalized(), api);
    }

    // Write operations - MorphTypeData
    public Task<MorphTypeData> CreateMorphTypeData(MorphTypeData morphType)
    {
        return _api.CreateMorphTypeData(morphType.Normalized());
    }

    public Task<MorphTypeData> UpdateMorphTypeData(Guid id, UpdateObjectInput<MorphTypeData> update)
    {
        return _api.UpdateMorphTypeData(id, update.Normalized());
    }

    public Task<MorphTypeData> UpdateMorphTypeData(MorphTypeData before, MorphTypeData after, IMiniLcmApi? api = null)
    {
        return _api.UpdateMorphTypeData(before.Normalized(), after.Normalized(), api);
    }

    void IDisposable.Dispose()
    {
    }
}
