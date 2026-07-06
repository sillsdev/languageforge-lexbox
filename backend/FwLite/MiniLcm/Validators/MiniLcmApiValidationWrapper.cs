using FluentValidation;
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
    [BeaKona.AutoInterface(IncludeBaseInterfaces = true, MemberMatch = BeaKona.MemberMatchTypes.Any)]
    private readonly IMiniLcmApi _api = api;

    // ********** Overrides go here **********

    public async Task<Publication> CreatePublication(Publication pub)
    {
        await validators.ValidateAndThrow(pub);
        if (pub.IsMain && await GetExistingMain() is not null)
            throw new ValidationException("Cannot create a second main publication. A main publication already exists.");
        return await _api.CreatePublication(pub);
    }

    public async Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        await validators.ValidateAndThrow(update);
        if (update.TryGetPropertyChange<Publication, bool>(nameof(Publication.IsMain), out var isMain) && isMain)
            await ThrowIfAnotherMainExists(id);
        return await _api.UpdatePublication(id, update);
    }

    public async Task SubmitUpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        await validators.ValidateAndThrow(update);
        if (update.TryGetPropertyChange<Publication, bool>(nameof(Publication.IsMain), out var isMain) && isMain)
            await ThrowIfAnotherMainExists(id);
        await _api.SubmitUpdatePublication(id, update);
    }

    public async Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        // This overload bypasses PublicationUpdateValidator, so enforce the single-main invariant here too.
        if (after.IsMain && !before.IsMain)
            await ThrowIfAnotherMainExists(after.Id);
        if (before.IsMain && !after.IsMain)
            throw new ValidationException("Cannot turn off the IsMain flag on a publication; the main publication is fixed.");
        return await _api.UpdatePublication(before, after, api ?? this);
    }

    private async Task ThrowIfAnotherMainExists(Guid id)
    {
        if (await GetExistingMain() is { } main && main.Id != id)
            throw new ValidationException("Cannot set IsMain on this publication. Another publication is already the main publication.");
    }

    private async Task<Publication?> GetExistingMain()
    {
        await foreach (var publication in _api.GetPublications())
        {
            if (publication.IsMain) return publication;
        }
        return null;
    }

    public async Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? between = null)
    {
        await validators.ValidateAndThrow(writingSystem);
        return await _api.CreateWritingSystem(writingSystem, between);
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

    public async Task<VariantType> CreateVariantType(VariantType variantType)
    {
        await validators.ValidateAndThrow(variantType);
        return await _api.CreateVariantType(variantType);
    }

    public async Task<VariantType> UpdateVariantType(VariantType before, VariantType after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateVariantType(before, after, api ?? this);
    }

    public async Task<Variant> CreateVariant(Variant variant)
    {
        await validators.ValidateAndThrow(variant);
        return await _api.CreateVariant(variant);
    }

    public async Task<Variant> UpdateVariant(Variant before, Variant after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateVariant(before, after, api ?? this);
    }

    // Submit* must be overridden explicitly: BeaKona's generated forwarders bypass the
    // interface default bodies and go straight to _api, silently skipping validation (#2362)
    public async Task SubmitCreateVariant(Variant variant)
    {
        await validators.ValidateAndThrow(variant);
        await _api.SubmitCreateVariant(variant);
    }

    public async Task SubmitUpdateVariant(Variant variant, UpdateObjectInput<Variant> update)
    {
        await validators.ValidateAndThrow(update);
        await _api.SubmitUpdateVariant(variant, update);
    }

    public async Task<MorphType> UpdateMorphType(Guid id, UpdateObjectInput<MorphType> update)
    {
        await validators.ValidateAndThrow(update);
        return await _api.UpdateMorphType(id, update);
    }

    public async Task<MorphType> UpdateMorphType(MorphType before, MorphType after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateMorphType(before, after, api ?? this);
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
