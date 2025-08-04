using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Wrappers;

namespace MiniLcm.Validators;

public class MiniLcmApiValidationWrapperFactory(MiniLcmValidators validators) : IMiniLcmWrapperFactory
{
    public IMiniLcmApi Create(IMiniLcmApi api, IProjectIdentifier _unused) => Create(api);

    public IMiniLcmApi Create(IMiniLcmApi api)
    {
        return new MiniLcmApiValidationWrapper(api, validators);
    }
}

public partial class MiniLcmApiValidationWrapper(
    IMiniLcmApi api,
    MiniLcmValidators validators) : IMiniLcmApi
{
    [BeaKona.AutoInterface(IncludeBaseInterfaces = true)]
    private readonly IMiniLcmApi _api = api;

    // ********** Overrides go here **********

    public async Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem)
    {
        await validators.ValidateAndThrow(writingSystem);
        return await _api.CreateWritingSystem(writingSystem);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update)
    {
        await validators.ValidateAndThrow(update);
        return await _api.UpdateWritingSystem(id, type, update);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateWritingSystem(before, after, api ?? this);
    }

    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        await validators.ValidateAndThrow(partOfSpeech);
        return await _api.CreatePartOfSpeech(partOfSpeech);
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdatePartOfSpeech(before, after, api ?? this);
    }

    public async Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        await validators.ValidateAndThrow(semanticDomain);
        return await _api.CreateSemanticDomain(semanticDomain);
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateSemanticDomain(before, after, api ?? this);
    }

    public async Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        await validators.ValidateAndThrow(complexFormType);
        return await _api.CreateComplexFormType(complexFormType);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateComplexFormType(before, after, api ?? this);
    }

    public async Task<Entry> CreateEntry(Entry entry)
    {
        await validators.ValidateAndThrow(entry);
        return await _api.CreateEntry(entry);
    }

    public async Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateEntry(before, after, api ?? this);
    }

    public async Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? between = null)
    {
        await validators.ValidateAndThrow(sense);
        return await _api.CreateSense(entryId, sense, between);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateSense(entryId, before, after, api ?? this);
    }

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence,
        BetweenPosition? between = null)
    {
        await validators.ValidateAndThrow(exampleSentence);
        return await _api.CreateExampleSentence(entryId, senseId, exampleSentence, between);
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after,
        IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateExampleSentence(entryId, senseId, before, after, api ?? this);
    }

    void IDisposable.Dispose()
    {
    }
}
