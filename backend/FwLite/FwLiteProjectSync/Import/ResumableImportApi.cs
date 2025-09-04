using System.Runtime.CompilerServices;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteProjectSync.Import;

public partial class ResumableImportApi(IMiniLcmApi api) : IMiniLcmApi
{
    [BeaKona.AutoInterface(IncludeBaseInterfaces = true, MemberMatch = BeaKona.MemberMatchTypes.Any)]
    private readonly IMiniLcmApi _api = api;
    private readonly Dictionary<string, Dictionary<string, object>> _createdObjects = new();
    private async ValueTask<T> HasCreated<T>(T value, IAsyncEnumerable<T> values, Func<Task<T>> create, [CallerMemberName] string typeName = "")
        where T : class, IObjectWithId
    {
        return await HasCreated(value, values, create, v => v.Id.ToString(), typeName);
    }
    private async ValueTask<T> HasCreated<T>(T value, IAsyncEnumerable<T> values, Func<Task<T>> create, Func<T, string> identity, [CallerMemberName] string typeName = "")
        where T : class
    {
        if (!_createdObjects.TryGetValue(typeName, out var created))
        {
            created = await values.ToDictionaryAsync(v => identity(v), v => (object)v);
            _createdObjects[typeName] = created;
        }
        var id = identity(value);
        if (created.TryGetValue(id, out var existing))
        {
            return (T)existing;
        }
        var createdValue = await create();
        created[id] = createdValue;
        return createdValue;
    }

    private async ValueTask<Dictionary<string, object>> EnsureCached(string typeName, IAsyncEnumerable<IObjectWithId> values)
    {
        if (!_createdObjects.TryGetValue(typeName, out var created))
        {
            created = await values.ToDictionaryAsync(v => v.Id.ToString(), v => (object)v);
            _createdObjects[typeName] = created;
        }
        return created;
    }

    // ********** Overrides go here **********

    async Task<Entry> IMiniLcmWriteApi.CreateEntry(Entry entry, CreateEntryOptions? options)
    {
        return await HasCreated(entry, _api.GetAllEntries(), () => _api.CreateEntry(entry, options));
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
    async Task<WritingSystem> IMiniLcmWriteApi.CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? between)
    {
        return await HasCreated(writingSystem, AsyncWs(), () => _api.CreateWritingSystem(writingSystem, between), ws => ws.Type + ws.WsId.Code);
    }

    private async IAsyncEnumerable<WritingSystem> AsyncWs()
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

    async Task IMiniLcmWriteApi.BulkImportSemanticDomains(IAsyncEnumerable<SemanticDomain> semanticDomains)
    {
        var cache = await EnsureCached(nameof(IMiniLcmApi.CreateSemanticDomain), _api.GetSemanticDomains());
        semanticDomains = semanticDomains.Where(sd => !cache.ContainsKey(sd.Id.ToString()));
        await _api.BulkImportSemanticDomains(semanticDomains);
    }

    async Task IMiniLcmWriteApi.BulkCreateEntries(IAsyncEnumerable<Entry> entries)
    {
        var cache = await EnsureCached(nameof(IMiniLcmApi.CreateEntry), _api.GetAllEntries());
        // Filter out entries, then call to Array so that if the LCM cache gets disposed during a long-running import, we already have the entries in memory.
        var list = await entries.Where(e => !cache.ContainsKey(e.Id.ToString())).ToArrayAsync();
        await _api.BulkCreateEntries(list.ToAsyncEnumerable());
    }
}
