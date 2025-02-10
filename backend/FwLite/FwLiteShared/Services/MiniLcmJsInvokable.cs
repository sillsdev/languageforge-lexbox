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
    ProjectEventBus projectEventBus) : IDisposable
{
    // TODO: Wrap MiniLcmApiNotifyWrapper around the api we receive so that JS calls will call it instead

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
    public async Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after)
    {
        var updatedWritingSystem = await api.UpdateWritingSystem(before, after);
        OnDataChanged();
        return updatedWritingSystem;
    }

    [JSInvokable]
    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        var createdPartOfSpeech = await api.CreatePartOfSpeech(partOfSpeech);
        OnDataChanged();
        return createdPartOfSpeech;
    }

    [JSInvokable]
    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after)
    {
        var updatedPartOfSpeech = await api.UpdatePartOfSpeech(before, after);
        OnDataChanged();
        return updatedPartOfSpeech;
    }

    [JSInvokable]
    public async Task DeletePartOfSpeech(Guid id)
    {
        await api.DeletePartOfSpeech(id);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        var createdSemanticDomain = await api.CreateSemanticDomain(semanticDomain);
        OnDataChanged();
        return createdSemanticDomain;
    }

    [JSInvokable]
    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after)
    {
        var updatedSemanticDomain = await api.UpdateSemanticDomain(before, after);
        OnDataChanged();
        return updatedSemanticDomain;
    }

    [JSInvokable]
    public async Task DeleteSemanticDomain(Guid id)
    {
        await api.DeleteSemanticDomain(id);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        var createdComplexFormType = await api.CreateComplexFormType(complexFormType);
        OnDataChanged();
        return createdComplexFormType;
    }

    [JSInvokable]
    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after)
    {
        var updatedComplexFormType = await api.UpdateComplexFormType(before, after);
        OnDataChanged();
        return updatedComplexFormType;
    }

    [JSInvokable]
    public async Task DeleteComplexFormType(Guid id)
    {
        await api.DeleteComplexFormType(id);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<Entry> CreateEntry(Entry entry)
    {
        var createdEntry = await api.CreateEntry(entry);
        OnDataChanged();
        projectEventBus.PublishEntryChangedEvent(project, createdEntry);
        return createdEntry;
    }

    [JSInvokable]
    public async Task<Entry> UpdateEntry(Entry before, Entry after)
    {
        //todo trigger sync on the test
        var result = await api.UpdateEntry(before, after);
        OnDataChanged();
        projectEventBus.PublishEntryChangedEvent(project, result);
        return result;
    }

    [JSInvokable]
    public async Task DeleteEntry(Guid id)
    {
        await api.DeleteEntry(id);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        var createdComplexFormComponent = await api.CreateComplexFormComponent(complexFormComponent);
        OnDataChanged();
        return createdComplexFormComponent;
    }

    [JSInvokable]
    public async Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await api.DeleteComplexFormComponent(complexFormComponent);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await api.AddComplexFormType(entryId, complexFormTypeId);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await api.RemoveComplexFormType(entryId, complexFormTypeId);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        var createdSense = await api.CreateSense(entryId, sense);
        OnDataChanged();
        return createdSense;
    }

    [JSInvokable]
    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after)
    {
        var updatedSense = await api.UpdateSense(entryId, before, after);
        OnDataChanged();
        return updatedSense;
    }

    [JSInvokable]
    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await api.DeleteSense(entryId, senseId);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        await api.AddSemanticDomainToSense(senseId, semanticDomain);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        await api.RemoveSemanticDomainFromSense(senseId, semanticDomainId);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence)
    {
        var createdExampleSentence = await api.CreateExampleSentence(entryId, senseId, exampleSentence);
        OnDataChanged();
        return createdExampleSentence;
    }

    [JSInvokable]
    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, ExampleSentence before, ExampleSentence after)
    {
        var updatedExampleSentence = await api.UpdateExampleSentence(entryId, senseId, before, after);
        OnDataChanged();
        return updatedExampleSentence;
    }

    [JSInvokable]
    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await api.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
        OnDataChanged();
    }

    public void Dispose()
    {
        api.Dispose();
    }
}
