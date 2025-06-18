using System.Runtime.CompilerServices;
using MiniLcm;
using MiniLcm.Models;

namespace FwLiteProjectSync.Import;

public partial class ResumableImportApi(IMiniLcmApi api) : IMiniLcmApi
{
    [BeaKona.AutoInterface(IncludeBaseInterfaces = true)]
    private readonly IMiniLcmApi _api = api;
    private readonly Dictionary<string, Dictionary<Guid, object>> _createdObjects = new();
    private async ValueTask<T> HasCreated<T>(T value, IAsyncEnumerable<T> values, Func<Task<T>> create, [CallerMemberName] string typeName = "")
        where T : class, IObjectWithId
    {
        if (!_createdObjects.TryGetValue(typeName, out var created))
        {
            created = await values.ToDictionaryAsync(v => v.Id, v => (object)v);
            _createdObjects[typeName] = created;
        }
        if (created.TryGetValue(value.Id, out var existing))
        {
            return (T)existing;
        }
        var createdValue = await create();
        created[value.Id] = createdValue;
        return createdValue;
    }

    // ********** Overrides go here **********

    async Task<Entry> IMiniLcmWriteApi.CreateEntry(Entry entry)
    {
        return await HasCreated(entry, _api.GetAllEntries(), () => _api.CreateEntry(entry));
    }

    async Task<PartOfSpeech> IMiniLcmWriteApi.CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        return await HasCreated(partOfSpeech, _api.GetPartsOfSpeech(), () => _api.CreatePartOfSpeech(partOfSpeech));
    }
    async Task<ComplexFormType> IMiniLcmWriteApi.CreateComplexFormType(ComplexFormType complexFormType)
    {
        return await HasCreated(complexFormType, _api.GetComplexFormTypes(), () => _api.CreateComplexFormType(complexFormType));
    }
    async Task<SemanticDomain> IMiniLcmWriteApi.CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        return await HasCreated(semanticDomain, _api.GetSemanticDomains(), () => _api.CreateSemanticDomain(semanticDomain));
    }
    async Task<Publication> IMiniLcmWriteApi.CreatePublication(Publication publication)
    {
        return await HasCreated(publication, _api.GetPublications(), () => _api.CreatePublication(publication));
    }
    async Task<WritingSystem> IMiniLcmWriteApi.CreateWritingSystem(WritingSystem writingSystem)
    {
        return await HasCreated(writingSystem, AsyncWs(), () => _api.CreateWritingSystem(writingSystem));
    }

    async IAsyncEnumerable<WritingSystem> AsyncWs()
    {
        var wss = await _api.GetWritingSystems();
        foreach (var ws in wss.Analysis)
        {
            yield return ws;
        }
        foreach (var ws in wss.Vernacular)
        {
            yield return ws;
        }
    }
}
