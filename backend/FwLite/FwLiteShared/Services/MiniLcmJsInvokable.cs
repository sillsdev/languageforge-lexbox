using FwLiteShared.Events;
using FwLiteShared.Sync;
using LcmCrdt;
using Microsoft.JSInterop;
using MiniLcm;
using MiniLcm.Models;

namespace FwLiteShared.Services;

public class MiniLcmJsInvokable(
    IMiniLcmApi api,
    BackgroundSyncService backgroundSyncService,
    IProjectIdentifier project,
    MiniLcmApiNotifyWrapperFactory apiWrapperFactory,
    ProjectEventBus projectEventBus) : IDisposable
{
    readonly IMiniLcmApi wrappedApi = apiWrapperFactory.Create(api, projectEventBus, project, api.GetEntry);

    public record MiniLcmFeatures(bool? History, bool? Write, bool? OpenWithFlex, bool? Feedback, bool? Sync);
    private bool SupportsSync => project.DataFormat == ProjectDataFormat.Harmony && api is CrdtMiniLcmApi;
    [JSInvokable]
    public MiniLcmFeatures SupportedFeatures()
    {
        var isCrdtProject = project.DataFormat == ProjectDataFormat.Harmony;
        var isFwDataProject = project.DataFormat == ProjectDataFormat.FwData;
        return new(History: isCrdtProject, Write: true, OpenWithFlex: isFwDataProject, Feedback: true, Sync: SupportsSync);
    }

    private void OnDataChanged()
    {
        // Do *not* check wrappedApi here
        if (api is IMiniLcmSaveApi saveApi)
        {
            saveApi.Save();
        }
        if (SupportsSync)
        {
            backgroundSyncService.TriggerSync(project);
        }
    }

    [JSInvokable]
    public Task<WritingSystems> GetWritingSystems()
    {
        return wrappedApi.GetWritingSystems();
    }

    [JSInvokable]
    public ValueTask<PartOfSpeech[]> GetPartsOfSpeech()
    {
        return wrappedApi.GetPartsOfSpeech().ToArrayAsync();
    }

    [JSInvokable]
    public ValueTask<SemanticDomain[]> GetSemanticDomains()
    {
        return wrappedApi.GetSemanticDomains().ToArrayAsync();
    }

    [JSInvokable]
    public ValueTask<ComplexFormType[]> GetComplexFormTypes()
    {
        return wrappedApi.GetComplexFormTypes().ToArrayAsync();
    }

    [JSInvokable]
    public Task<ComplexFormType?> GetComplexFormType(Guid id)
    {
        return wrappedApi.GetComplexFormType(id);
    }

    [JSInvokable]
    public ValueTask<Entry[]> GetEntries(QueryOptions? options = null)
    {
        return wrappedApi.GetEntries(options).ToArrayAsync();
    }

    [JSInvokable]
    public ValueTask<Entry[]> SearchEntries(string query, QueryOptions? options = null)
    {
        return wrappedApi.SearchEntries(query, options).ToArrayAsync();
    }

    [JSInvokable]
    public Task<Entry?> GetEntry(Guid id)
    {
        return wrappedApi.GetEntry(id);
    }

    [JSInvokable]
    public Task<Sense?> GetSense(Guid entryId, Guid id)
    {
        return wrappedApi.GetSense(entryId, id);
    }

    [JSInvokable]
    public Task<PartOfSpeech?> GetPartOfSpeech(Guid id)
    {
        return wrappedApi.GetPartOfSpeech(id);
    }

    [JSInvokable]
    public Task<SemanticDomain?> GetSemanticDomain(Guid id)
    {
        return wrappedApi.GetSemanticDomain(id);
    }

    [JSInvokable]
    public Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id)
    {
        return wrappedApi.GetExampleSentence(entryId, senseId, id);
    }

    [JSInvokable]
    public Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem)
    {
        return wrappedApi.CreateWritingSystem(type, writingSystem);
    }

    [JSInvokable]
    public async Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after)
    {
        var updatedWritingSystem = await wrappedApi.UpdateWritingSystem(before, after);
        OnDataChanged();
        return updatedWritingSystem;
    }

    [JSInvokable]
    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        var createdPartOfSpeech = await wrappedApi.CreatePartOfSpeech(partOfSpeech);
        OnDataChanged();
        return createdPartOfSpeech;
    }

    [JSInvokable]
    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after)
    {
        var updatedPartOfSpeech = await wrappedApi.UpdatePartOfSpeech(before, after);
        OnDataChanged();
        return updatedPartOfSpeech;
    }

    [JSInvokable]
    public async Task DeletePartOfSpeech(Guid id)
    {
        await wrappedApi.DeletePartOfSpeech(id);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        var createdSemanticDomain = await wrappedApi.CreateSemanticDomain(semanticDomain);
        OnDataChanged();
        return createdSemanticDomain;
    }

    [JSInvokable]
    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after)
    {
        var updatedSemanticDomain = await wrappedApi.UpdateSemanticDomain(before, after);
        OnDataChanged();
        return updatedSemanticDomain;
    }

    [JSInvokable]
    public async Task DeleteSemanticDomain(Guid id)
    {
        await wrappedApi.DeleteSemanticDomain(id);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        var createdComplexFormType = await wrappedApi.CreateComplexFormType(complexFormType);
        OnDataChanged();
        return createdComplexFormType;
    }

    [JSInvokable]
    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after)
    {
        var updatedComplexFormType = await wrappedApi.UpdateComplexFormType(before, after);
        OnDataChanged();
        return updatedComplexFormType;
    }

    [JSInvokable]
    public async Task DeleteComplexFormType(Guid id)
    {
        await wrappedApi.DeleteComplexFormType(id);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<Entry> CreateEntry(Entry entry)
    {
        var createdEntry = await wrappedApi.CreateEntry(entry);
        OnDataChanged();
        projectEventBus.PublishEntryChangedEvent(project, createdEntry);
        return createdEntry;
    }

    [JSInvokable]
    public async Task<Entry> UpdateEntry(Entry before, Entry after)
    {
        //todo trigger sync on the test
        var result = await wrappedApi.UpdateEntry(before, after);
        OnDataChanged();
        projectEventBus.PublishEntryChangedEvent(project, result);
        return result;
    }

    [JSInvokable]
    public async Task DeleteEntry(Guid id)
    {
        await wrappedApi.DeleteEntry(id);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        var createdComplexFormComponent = await wrappedApi.CreateComplexFormComponent(complexFormComponent);
        OnDataChanged();
        return createdComplexFormComponent;
    }

    [JSInvokable]
    public async Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await wrappedApi.DeleteComplexFormComponent(complexFormComponent);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await wrappedApi.AddComplexFormType(entryId, complexFormTypeId);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await wrappedApi.RemoveComplexFormType(entryId, complexFormTypeId);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        var createdSense = await wrappedApi.CreateSense(entryId, sense);
        OnDataChanged();
        return createdSense;
    }

    [JSInvokable]
    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after)
    {
        var updatedSense = await wrappedApi.UpdateSense(entryId, before, after);
        OnDataChanged();
        return updatedSense;
    }

    [JSInvokable]
    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await wrappedApi.DeleteSense(entryId, senseId);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        await wrappedApi.AddSemanticDomainToSense(senseId, semanticDomain);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        await wrappedApi.RemoveSemanticDomainFromSense(senseId, semanticDomainId);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence)
    {
        var createdExampleSentence = await wrappedApi.CreateExampleSentence(entryId, senseId, exampleSentence);
        OnDataChanged();
        return createdExampleSentence;
    }

    [JSInvokable]
    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, ExampleSentence before, ExampleSentence after)
    {
        var updatedExampleSentence = await wrappedApi.UpdateExampleSentence(entryId, senseId, before, after);
        OnDataChanged();
        return updatedExampleSentence;
    }

    [JSInvokable]
    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await wrappedApi.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
        OnDataChanged();
    }

    public void Dispose()
    {
        wrappedApi.Dispose();
        // Note we do *not* call .Dispose() on the api handed to us in the constructor param, that's the job of the DI container
    }
}
