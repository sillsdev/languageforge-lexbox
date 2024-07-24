using System.Text.Json;
using FwDataMiniLcmBridge.Api;
using LcmCrdt;
using Microsoft.Extensions.Options;
using MiniLcm;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace FwLiteProjectSync;

public class CrdtFwdataProjectSyncService(IOptions<LcmCrdtConfig> lcmCrdtConfig, MiniLcmImport miniLcmImport)
{
    public record SyncResult(int CrdtChanges, int FwdataChanges);

    public async Task<SyncResult> Sync(ILexboxApi crdtApi, FwDataMiniLcmApi fwdataApi)
    {
        var projectSnapshot = await GetProjectSnapshot(fwdataApi.Project.Name);
        SyncResult result;
        if (projectSnapshot is null)
        {
            await miniLcmImport.ImportProject(crdtApi, fwdataApi, fwdataApi.EntryCount);
            result = new SyncResult(fwdataApi.EntryCount, 0);
        }
        else
        {
            int crdtChanges = 0;
            int fwdataChanges = 0;
            var currentFwDataEntries = await fwdataApi.GetEntries().ToArrayAsync();
            await EntrySync(currentFwDataEntries, projectSnapshot.Entries, crdtApi);

            await EntrySync(await crdtApi.GetEntries().ToArrayAsync(), currentFwDataEntries, fwdataApi);


            result = new SyncResult(crdtChanges, fwdataChanges);
        }

        await SaveProjectSnapshot(fwdataApi.Project.Name,
            new ProjectSnapshot(await fwdataApi.GetEntries().ToArrayAsync()));
        return result;
    }

    public record ProjectSnapshot(Entry[] Entries);

    private async Task<ProjectSnapshot?> GetProjectSnapshot(string projectName)
    {
        var snapshotPath = Path.Combine(lcmCrdtConfig.Value.ProjectPath, $"{projectName}_snapshot.json");
        if (!File.Exists(snapshotPath)) return null;
        await using var file = File.OpenRead(snapshotPath);
        return await JsonSerializer.DeserializeAsync<ProjectSnapshot>(file);
    }

    private async Task SaveProjectSnapshot(string projectName, ProjectSnapshot projectSnapshot)
    {
        var snapshotPath = Path.Combine(lcmCrdtConfig.Value.ProjectPath, $"{projectName}_snapshot.json");
        await using var file = File.Create(snapshotPath);
        await JsonSerializer.SerializeAsync(file, projectSnapshot);
    }

    private async Task EntrySync(Entry[] currentEntries,
        Entry[] previousEntries,
        ILexboxApi api)
    {
        await DiffCollection(api,
            previousEntries,
            currentEntries,
            async (api, currentEntry) => await api.CreateEntry(currentEntry),
            async (api, previousEntry) => await api.DeleteEntry(previousEntry.Id),
            async (api, previousEntry, currentEntry) =>
            {
                var updateObjectInput = EntryDiffToUpdate(previousEntry, currentEntry);
                if (updateObjectInput is not null) await api.UpdateEntry(currentEntry.Id, updateObjectInput);
                await SenseSync(currentEntry.Id, currentEntry.Senses, previousEntry.Senses, api);
            });
    }

    private async Task SenseSync(Guid entryId,
        IList<Sense> currentSenses,
        IList<Sense> previousSenses,
        ILexboxApi api)
    {
        await DiffCollection(api,
            previousSenses,
            currentSenses,
            async (api, currentSense) => await api.CreateSense(entryId, currentSense),
            async (api, previousSense) => await api.DeleteSense(entryId, previousSense.Id),
            async (api, previousSense, currentSense) =>
            {
                var updateObjectInput = await SenseDiffToUpdate(previousSense, currentSense);
                if (updateObjectInput is not null) await api.UpdateSense(entryId, previousSense.Id, updateObjectInput);
                await ExampleSentenceSync(entryId,
                    previousSense.Id,
                    currentSense.ExampleSentences,
                    previousSense.ExampleSentences,
                    api);
            });
    }

    private async Task ExampleSentenceSync(Guid entryId,
        Guid senseId,
        IList<ExampleSentence> currentExampleSentences,
        IList<ExampleSentence> previousExampleSentences,
        ILexboxApi api)
    {
        await DiffCollection(api,
            previousExampleSentences,
            currentExampleSentences,
            async (api, currentExampleSentence) =>
                await api.CreateExampleSentence(entryId, senseId, currentExampleSentence),
            async (api, previousExampleSentence) =>
                await api.DeleteExampleSentence(entryId, senseId, previousExampleSentence.Id),
            async (api, previousExampleSentence, currentExampleSentence) =>
            {
                var updateObjectInput = ExampleDiffToUpdate(previousExampleSentence, currentExampleSentence);
                if (updateObjectInput is not null) await api.UpdateExampleSentence(entryId, senseId, previousExampleSentence.Id, updateObjectInput);
            });
    }

    public static UpdateObjectInput<Entry>? EntryDiffToUpdate(Entry previousEntry, Entry currentEntry)
    {
        JsonPatchDocument<Entry> patchDocument = new();
        patchDocument.Operations.AddRange(GetMultiStringDiff<Entry>(nameof(Entry.LexemeForm),
            previousEntry.LexemeForm,
            currentEntry.LexemeForm));
        patchDocument.Operations.AddRange(GetMultiStringDiff<Entry>(nameof(Entry.CitationForm),
            previousEntry.CitationForm,
            currentEntry.CitationForm));
        patchDocument.Operations.AddRange(GetMultiStringDiff<Entry>(nameof(Entry.Note),
            previousEntry.Note,
            currentEntry.Note));
        patchDocument.Operations.AddRange(GetMultiStringDiff<Entry>(nameof(Entry.LiteralMeaning),
            previousEntry.LiteralMeaning,
            currentEntry.LiteralMeaning));
        if (patchDocument.Operations.Count == 0) return null;
        return new JsonPatchUpdateInput<Entry>(patchDocument);
    }

    public static async Task<UpdateObjectInput<Sense>?> SenseDiffToUpdate(Sense previousSense, Sense currentSense)
    {
        JsonPatchDocument<Sense> patchDocument = new();
        patchDocument.Operations.AddRange(GetMultiStringDiff<Sense>(nameof(Sense.Gloss),
            previousSense.Gloss,
            currentSense.Gloss));
        patchDocument.Operations.AddRange(GetMultiStringDiff<Sense>(nameof(Sense.Definition),
            previousSense.Definition,
            currentSense.Definition));
        if (previousSense.PartOfSpeech != currentSense.PartOfSpeech)
        {
            patchDocument.Replace(sense => sense.PartOfSpeech, currentSense.PartOfSpeech);
        }

        if (previousSense.PartOfSpeechId != currentSense.PartOfSpeechId)
        {
            patchDocument.Replace(sense => sense.PartOfSpeechId, currentSense.PartOfSpeechId);
        }

        await DiffCollection(null!,
            previousSense.SemanticDomains,
            currentSense.SemanticDomains,
            (_, domain) =>
            {
                patchDocument.Add(sense => sense.SemanticDomains, domain);
                return Task.CompletedTask;
            },
            (_, previousDomain) =>
            {
                patchDocument.Remove(sense => sense.SemanticDomains, previousSense.SemanticDomains.IndexOf(previousDomain));
                return Task.CompletedTask;
            },
            (_, previousDomain, currentDomain) =>
            {
                //do nothing, semantic domains are not editable here
                return Task.CompletedTask;
            });
        if (patchDocument.Operations.Count == 0) return null;
        return new JsonPatchUpdateInput<Sense>(patchDocument);
    }

    public static UpdateObjectInput<ExampleSentence>? ExampleDiffToUpdate(ExampleSentence previousExampleSentence,
        ExampleSentence currentExampleSentence)
    {
        JsonPatchDocument<ExampleSentence> patchDocument = new();
        patchDocument.Operations.AddRange(GetMultiStringDiff<ExampleSentence>(nameof(ExampleSentence.Sentence),
            previousExampleSentence.Sentence,
            currentExampleSentence.Sentence));
        patchDocument.Operations.AddRange(GetMultiStringDiff<ExampleSentence>(nameof(ExampleSentence.Translation),
            previousExampleSentence.Translation,
            currentExampleSentence.Translation));
        if (previousExampleSentence.Reference != currentExampleSentence.Reference)
        {
            patchDocument.Replace(exampleSentence => exampleSentence.Reference, currentExampleSentence.Reference);
        }

        if (patchDocument.Operations.Count == 0) return null;
        return new JsonPatchUpdateInput<ExampleSentence>(patchDocument);
    }

    public static IEnumerable<Operation<T>> GetMultiStringDiff<T>(string path, MultiString previous, MultiString current)
        where T : class
    {
        var currentKeys = current.Values.Keys.ToHashSet();
        foreach (var (key, previousValue) in previous.Values)
        {
            if (current.Values.TryGetValue(key, out var currentValue))
            {
                if (!previousValue.Equals(currentValue))
                    yield return new Operation<T>("replace", $"/{path}/{key}", null, currentValue);
            }
            else
            {
                yield return new Operation<T>("remove", $"/{path}/{key}", null);
            }

            currentKeys.Remove(key);
        }

        foreach (var key in currentKeys)
        {
            yield return new Operation<T>("add", $"/{path}/{key}", null, current.Values[key]);
        }
    }

    private static async Task DiffCollection<T>(
        ILexboxApi api,
        IList<T> previous,
        IList<T> current,
        Func<ILexboxApi, T, Task> add,
        Func<ILexboxApi, T, Task> remove,
        Func<ILexboxApi, T, T, Task> replace) where T : IObjectWithId
    {
        var currentEntriesDict = current.ToDictionary(entry => entry.Id);
        foreach (var previousEntry in previous)
        {
            if (currentEntriesDict.TryGetValue(previousEntry.Id, out var currentEntry))
            {
                await replace(api, previousEntry, currentEntry);
            }
            else
            {
                await remove(api, previousEntry);
            }

            currentEntriesDict.Remove(previousEntry.Id);
        }

        foreach (var value in currentEntriesDict.Values)
        {
            await add(api, value);
        }
    }
}
