using FwDataMiniLcmBridge.Api;
using LcmCrdt;
using Microsoft.JSInterop;
using MiniLcm;
using MiniLcm.Models;

namespace FwLiteShared.Services;

internal class MiniLcmJsInvokable(IMiniLcmApi api)
{
    public record MiniLcmFeatures(bool History, bool Write, bool OpenWithFlex, bool Feedback, bool Sync);
    [JSInvokable]
    public MiniLcmFeatures SupportedFeatures()
    {
        var isCrdtProject = api is CrdtMiniLcmApi;
        var isFwDataProject = api is FwDataMiniLcmApi;
        return new(History: isCrdtProject, Write: true, OpenWithFlex: isFwDataProject, Feedback: true, Sync: isCrdtProject);
    }

    [JSInvokable]
    public Task<WritingSystems> GetWritingSystems()
    {
        return api.GetWritingSystems();
    }

    [JSInvokable]
    public ValueTask<PartOfSpeech[]> GetPartsOfSpeech()
    {
        return api.GetPartsOfSpeech().ToArrayAsync();
    }

    [JSInvokable]
    public ValueTask<SemanticDomain[]> GetSemanticDomains()
    {
        return api.GetSemanticDomains().ToArrayAsync();
    }

    [JSInvokable]
    public ValueTask<ComplexFormType[]> GetComplexFormTypes()
    {
        return api.GetComplexFormTypes().ToArrayAsync();
    }

    [JSInvokable]
    public Task<ComplexFormType?> GetComplexFormType(Guid id)
    {
        return api.GetComplexFormType(id);
    }

    [JSInvokable]
    public ValueTask<Entry[]> GetEntries(QueryOptions? options = null)
    {
        return api.GetEntries(options).ToArrayAsync();
    }

    [JSInvokable]
    public ValueTask<Entry[]> SearchEntries(string query, QueryOptions? options = null)
    {
        return api.SearchEntries(query, options).ToArrayAsync();
    }

    [JSInvokable]
    public Task<Entry?> GetEntry(Guid id)
    {
        return api.GetEntry(id);
    }

    [JSInvokable]
    public Task<Sense?> GetSense(Guid entryId, Guid id)
    {
        return api.GetSense(entryId, id);
    }

    [JSInvokable]
    public Task<PartOfSpeech?> GetPartOfSpeech(Guid id)
    {
        return api.GetPartOfSpeech(id);
    }

    [JSInvokable]
    public Task<SemanticDomain?> GetSemanticDomain(Guid id)
    {
        return api.GetSemanticDomain(id);
    }

    [JSInvokable]
    public Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id)
    {
        return api.GetExampleSentence(entryId, senseId, id);
    }

    [JSInvokable]
    public Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem)
    {
        return api.CreateWritingSystem(type, writingSystem);
    }

    [JSInvokable]
    public Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after)
    {
        return api.UpdateWritingSystem(before, after);
    }

    [JSInvokable]
    public Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        return api.CreatePartOfSpeech(partOfSpeech);
    }

    [JSInvokable]
    public Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after)
    {
        return api.UpdatePartOfSpeech(before, after);
    }

    [JSInvokable]
    public Task DeletePartOfSpeech(Guid id)
    {
        return api.DeletePartOfSpeech(id);
    }

    [JSInvokable]
    public Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        return api.CreateSemanticDomain(semanticDomain);
    }

    [JSInvokable]
    public Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after)
    {
        return api.UpdateSemanticDomain(before, after);
    }

    [JSInvokable]
    public Task DeleteSemanticDomain(Guid id)
    {
        return api.DeleteSemanticDomain(id);
    }

    [JSInvokable]
    public Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        return api.CreateComplexFormType(complexFormType);
    }

    [JSInvokable]
    public Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after)
    {
        return api.UpdateComplexFormType(before, after);
    }

    [JSInvokable]
    public Task DeleteComplexFormType(Guid id)
    {
        return api.DeleteComplexFormType(id);
    }

    [JSInvokable]
    public Task<Entry> CreateEntry(Entry entry)
    {
        return api.CreateEntry(entry);
    }

    [JSInvokable]
    public Task<Entry> UpdateEntry(Entry before, Entry after)
    {
        return api.UpdateEntry(before, after);
    }

    [JSInvokable]
    public Task DeleteEntry(Guid id)
    {
        return api.DeleteEntry(id);
    }

    [JSInvokable]
    public Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        return api.CreateComplexFormComponent(complexFormComponent);
    }

    [JSInvokable]
    public Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        return api.DeleteComplexFormComponent(complexFormComponent);
    }

    [JSInvokable]
    public Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        return api.AddComplexFormType(entryId, complexFormTypeId);
    }

    [JSInvokable]
    public Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        return api.RemoveComplexFormType(entryId, complexFormTypeId);
    }

    [JSInvokable]
    public Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        return api.CreateSense(entryId, sense);
    }

    [JSInvokable]
    public Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after)
    {
        return api.UpdateSense(entryId, before, after);
    }

    [JSInvokable]
    public Task DeleteSense(Guid entryId, Guid senseId)
    {
        return api.DeleteSense(entryId, senseId);
    }

    [JSInvokable]
    public Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        return api.AddSemanticDomainToSense(senseId, semanticDomain);
    }

    [JSInvokable]
    public Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        return api.RemoveSemanticDomainFromSense(senseId, semanticDomainId);
    }

    [JSInvokable]
    public Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence)
    {
        return api.CreateExampleSentence(entryId, senseId, exampleSentence);
    }

    [JSInvokable]
    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, ExampleSentence before, ExampleSentence after)
    {
        return api.UpdateExampleSentence(entryId, senseId, before, after);
    }

    [JSInvokable]
    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        return api.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
    }
}
