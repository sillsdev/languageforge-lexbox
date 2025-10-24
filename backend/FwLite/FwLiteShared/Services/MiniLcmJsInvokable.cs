using FwLiteShared.Sync;
using LcmCrdt;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MiniLcm;
using MiniLcm.Media;
using MiniLcm.Models;
using MiniLcm.Normalization;
using MiniLcm.Validators;
using MiniLcm.Wrappers;
using Reinforced.Typings.Attributes;

namespace FwLiteShared.Services;

public class MiniLcmJsInvokable(
    IMiniLcmApi api,
    BackgroundSyncService backgroundSyncService,
    IProjectIdentifier project,
    ILogger<MiniLcmJsInvokable> logger,
    MiniLcmApiNotifyWrapperFactory notificationWrapperFactory,
    MiniLcmApiValidationWrapperFactory validationWrapperFactory,
    MiniLcmApiStringNormalizationWrapperFactory normalizationWrapperFactory
    ) : IDisposable
{
    private readonly IMiniLcmApi _wrappedApi = api.WrapWith([normalizationWrapperFactory, validationWrapperFactory, notificationWrapperFactory], project);

    public record MiniLcmFeatures(bool? History, bool? Write, bool? OpenWithFlex, bool? Feedback, bool? Sync, bool? Audio);
    private bool SupportsSync => project.DataFormat == ProjectDataFormat.Harmony && api is CrdtMiniLcmApi;
    [JSInvokable]
    public MiniLcmFeatures SupportedFeatures()
    {
        var isCrdtProject = project.DataFormat == ProjectDataFormat.Harmony;
        var isFwDataProject = project.DataFormat == ProjectDataFormat.FwData;
        return new(History: isCrdtProject, Write: CanWrite, OpenWithFlex: isFwDataProject, Feedback: true, Sync: SupportsSync, Audio: true);
    }

    private bool CanWrite =>
        project is not CrdtProject crdt ||
        (crdt.Data?.Role ?? UserProjectRole.Editor) is UserProjectRole.Editor or UserProjectRole.Manager;

    //todo move info notify wrapper factory
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
        return _wrappedApi.GetWritingSystems();
    }

    [JSInvokable]
    public ValueTask<PartOfSpeech[]> GetPartsOfSpeech()
    {
        return _wrappedApi.GetPartsOfSpeech().ToArrayAsync();
    }

    [JSInvokable]
    public ValueTask<Publication[]> GetPublications()
    {
        return _wrappedApi.GetPublications().ToArrayAsync();
    }

    [JSInvokable]
    public Task<SemanticDomain[]> GetSemanticDomains()
    {
        return Task.Run(async () => await _wrappedApi.GetSemanticDomains().ToArrayAsync());
    }

    [JSInvokable]
    public ValueTask<ComplexFormType[]> GetComplexFormTypes()
    {
        return _wrappedApi.GetComplexFormTypes().ToArrayAsync();
    }

    [JSInvokable]
    [TsFunction(Type = "Promise<IComplexFormType | null>")]
    public Task<ComplexFormType?> GetComplexFormType(Guid id)
    {
        return _wrappedApi.GetComplexFormType(id);
    }

    [JSInvokable]
    public Task<int> CountEntries(string? query, FilterQueryOptions? options)
    {
        return Task.Run(() => _wrappedApi.CountEntries(query, options));
    }

    [JSInvokable]
    public Task<Entry[]> GetEntries(QueryOptions? options = null)
    {
        return Task.Run(async () => await _wrappedApi.GetEntries(options).ToArrayAsync());
    }

    [JSInvokable]
    public Task<Entry[]> SearchEntries(string query, QueryOptions? options = null)
    {
        return Task.Run(async () => await _wrappedApi.SearchEntries(query, options).ToArrayAsync());
    }

    [JSInvokable]
    [TsFunction(Type = "Promise<IEntry | null>")]
    public Task<Entry?> GetEntry(Guid id)
    {
        return Task.Run(async () => await _wrappedApi.GetEntry(id));
    }

    [JSInvokable]
    [TsFunction(Type = "Promise<ISense | null>")]
    public Task<Sense?> GetSense(Guid entryId, Guid id)
    {
        return _wrappedApi.GetSense(entryId, id);
    }

    [JSInvokable]
    [TsFunction(Type = "Promise<IPartOfSpeech | null>")]
    public Task<PartOfSpeech?> GetPartOfSpeech(Guid id)
    {
        return _wrappedApi.GetPartOfSpeech(id);
    }

    [JSInvokable]
    [TsFunction(Type = "Promise<ISemanticDomain | null>")]
    public Task<SemanticDomain?> GetSemanticDomain(Guid id)
    {
        return _wrappedApi.GetSemanticDomain(id);
    }

    [JSInvokable]
    [TsFunction(Type = "Promise<IExampleSentence | null>")]
    public Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id)
    {
        return _wrappedApi.GetExampleSentence(entryId, senseId, id);
    }

    [JSInvokable]
    public Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem)
    {
        return _wrappedApi.CreateWritingSystem(writingSystem);
    }

    [JSInvokable]
    public async Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after)
    {
        var updatedWritingSystem = await _wrappedApi.UpdateWritingSystem(before, after);
        OnDataChanged();
        return updatedWritingSystem;
    }

    [JSInvokable]
    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        var createdPartOfSpeech = await _wrappedApi.CreatePartOfSpeech(partOfSpeech);
        OnDataChanged();
        return createdPartOfSpeech;
    }

    [JSInvokable]
    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after)
    {
        var updatedPartOfSpeech = await _wrappedApi.UpdatePartOfSpeech(before, after);
        OnDataChanged();
        return updatedPartOfSpeech;
    }

    [JSInvokable]
    public async Task DeletePartOfSpeech(Guid id)
    {
        await _wrappedApi.DeletePartOfSpeech(id);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        var createdSemanticDomain = await _wrappedApi.CreateSemanticDomain(semanticDomain);
        OnDataChanged();
        return createdSemanticDomain;
    }

    [JSInvokable]
    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after)
    {
        var updatedSemanticDomain = await _wrappedApi.UpdateSemanticDomain(before, after);
        OnDataChanged();
        return updatedSemanticDomain;
    }

    [JSInvokable]
    public async Task DeleteSemanticDomain(Guid id)
    {
        await _wrappedApi.DeleteSemanticDomain(id);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        var createdComplexFormType = await _wrappedApi.CreateComplexFormType(complexFormType);
        OnDataChanged();
        return createdComplexFormType;
    }

    [JSInvokable]
    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after)
    {
        var updatedComplexFormType = await _wrappedApi.UpdateComplexFormType(before, after);
        OnDataChanged();
        return updatedComplexFormType;
    }

    [JSInvokable]
    public async Task DeleteComplexFormType(Guid id)
    {
        await _wrappedApi.DeleteComplexFormType(id);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<Entry> CreateEntry(Entry entry)
    {
        var createdEntry = await _wrappedApi.CreateEntry(entry);
        OnDataChanged();
        return createdEntry;
    }

    [JSInvokable]
    public async Task<Entry> UpdateEntry(Entry before, Entry after)
    {
        var result = await Task.Run(async () => await _wrappedApi.UpdateEntry(before, after));
        OnDataChanged();
        return result;
    }

    [JSInvokable]
    public async Task DeleteEntry(Guid id)
    {
        await _wrappedApi.DeleteEntry(id);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        var createdComplexFormComponent = await _wrappedApi.CreateComplexFormComponent(complexFormComponent);
        OnDataChanged();
        return createdComplexFormComponent;
    }

    [JSInvokable]
    public async Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await _wrappedApi.DeleteComplexFormComponent(complexFormComponent);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await _wrappedApi.AddComplexFormType(entryId, complexFormTypeId);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await _wrappedApi.RemoveComplexFormType(entryId, complexFormTypeId);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        var createdSense = await _wrappedApi.CreateSense(entryId, sense);
        OnDataChanged();
        return createdSense;
    }

    [JSInvokable]
    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after)
    {
        var updatedSense = await _wrappedApi.UpdateSense(entryId, before, after);
        OnDataChanged();
        return updatedSense;
    }

    [JSInvokable]
    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await _wrappedApi.DeleteSense(entryId, senseId);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        await _wrappedApi.AddSemanticDomainToSense(senseId, semanticDomain);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        await _wrappedApi.RemoveSemanticDomainFromSense(senseId, semanticDomainId);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence)
    {
        var createdExampleSentence = await _wrappedApi.CreateExampleSentence(entryId, senseId, exampleSentence);
        OnDataChanged();
        return createdExampleSentence;
    }

    [JSInvokable]
    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, ExampleSentence before, ExampleSentence after)
    {
        var updatedExampleSentence = await _wrappedApi.UpdateExampleSentence(entryId, senseId, before, after);
        OnDataChanged();
        return updatedExampleSentence;
    }

    [JSInvokable]
    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await _wrappedApi.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
        OnDataChanged();
    }

    [JSInvokable]
    public async Task<ReadFileResponseJs?> GetFileStream(string mediaUri)
    {
        var result = await _wrappedApi.GetFileStream(new MediaUri(mediaUri));
        var stream = result.Stream is null ? null : new DotNetStreamReference(result.Stream);
        return new ReadFileResponseJs(stream, result.FileName, result.Result, result.ErrorMessage);
    }

    public record ReadFileResponseJs(
        DotNetStreamReference? Stream,
        string? FileName,
        ReadFileResult Result,
        string? ErrorMessage);
    public const int TenMbFileLimit = 10 * 1024 * 1024;

    [JSInvokable]
    public Task<UploadFileResponse> SaveFile(IJSStreamReference streamReference, LcmFileMetadata metadata)
    {
        if (streamReference.Length > TenMbFileLimit)
            return Task.FromResult(new UploadFileResponse(UploadFileResult.TooBig));

        return Task.Run(async () =>
        {
            await using var stream = await streamReference.OpenReadStreamAsync(TenMbFileLimit);
            var result = await _wrappedApi.SaveFile(stream, metadata);
            try
            {
                await streamReference.DisposeAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error disposing stream reference");
            }
            return result;
        });
    }

    public void Dispose()
    {
        _wrappedApi.Dispose();
        // Note we do *not* call .Dispose() on the api handed to us in the constructor param, that's the job of the DI container
    }
}
