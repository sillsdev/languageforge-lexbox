using System.Text.Json;
using FwDataMiniLcmBridge.Api;
using LcmCrdt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace FwLiteProjectSync;

public class CrdtFwdataProjectSyncService(IOptions<LcmCrdtConfig> lcmCrdtConfig, MiniLcmImport miniLcmImport, ILogger<CrdtFwdataProjectSyncService> logger)
{
    public record SyncResult(int CrdtChanges, int FwdataChanges);

    public async Task<SyncResult> Sync(ILexboxApi crdtApi, FwDataMiniLcmApi fwdataApi, bool dryRun = false)
    {
        var projectSnapshot = await GetProjectSnapshot(fwdataApi.Project.Name);
        SyncResult result = await Sync(crdtApi, fwdataApi, dryRun, fwdataApi.EntryCount, projectSnapshot);

        if (!dryRun)
        {
            await SaveProjectSnapshot(fwdataApi.Project.Name,
                new ProjectSnapshot(await fwdataApi.GetEntries().ToArrayAsync()));
        }
        return result;
    }

    private async Task<SyncResult> Sync(ILexboxApi crdtApi, ILexboxApi fwdataApi, bool dryRun, int entryCount, ProjectSnapshot? projectSnapshot)
    {
        if (dryRun)
        {
            crdtApi = new DryRunMiniLcmApi(crdtApi);
            fwdataApi = new DryRunMiniLcmApi(fwdataApi);
        }

        if (projectSnapshot is null)
        {
            await miniLcmImport.ImportProject(crdtApi, fwdataApi, entryCount);
            LogDryRun(crdtApi, "crdt");
            return new SyncResult(entryCount, 0);
        }

        var currentFwDataEntries = await fwdataApi.GetEntries().ToArrayAsync();
        var crdtChanges = await EntrySync(currentFwDataEntries, projectSnapshot.Entries, crdtApi);
        LogDryRun(crdtApi, "crdt");

        var fwdataChanges = await EntrySync(await crdtApi.GetEntries().ToArrayAsync(), currentFwDataEntries, fwdataApi);
        LogDryRun(fwdataApi, "fwdata");

        //todo push crdt changes to lexbox

        return new SyncResult(crdtChanges, fwdataChanges);
    }

    private void LogDryRun(ILexboxApi api, string type)
    {
        if (api is not DryRunMiniLcmApi dryRunApi) return;
        foreach (var dryRunRecord in dryRunApi.DryRunRecords)
        {
            logger.LogInformation($"Dry run {type} record: {dryRunRecord.Method} {dryRunRecord.Description}");
        }

        logger.LogInformation($"Dry run {type} changes: {dryRunApi.DryRunRecords.Count}");
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

    private async Task<int> EntrySync(Entry[] currentEntries,
        Entry[] previousEntries,
        ILexboxApi api)
    {
        return await DiffCollection(api,
            previousEntries,
            currentEntries,
            async (api, currentEntry) =>
            {
                await api.CreateEntry(currentEntry);
                return 1;
            },
            async (api, previousEntry) =>
            {
                await api.DeleteEntry(previousEntry.Id);
                return 1;
            },
            async (api, previousEntry, currentEntry) =>
            {
                var updateObjectInput = EntryDiffToUpdate(previousEntry, currentEntry);
                if (updateObjectInput is not null) await api.UpdateEntry(currentEntry.Id, updateObjectInput);
                var changes = await SenseSync(currentEntry.Id, currentEntry.Senses, previousEntry.Senses, api);
                return changes + (updateObjectInput is null ? 0 : 1);
            });
    }

    private async Task<int> SenseSync(Guid entryId,
        IList<Sense> currentSenses,
        IList<Sense> previousSenses,
        ILexboxApi api)
    {
        return await DiffCollection(api,
            previousSenses,
            currentSenses,
            async (api, currentSense) =>
            {
                await api.CreateSense(entryId, currentSense);
                return 1;
            },
            async (api, previousSense) =>
            {
                await api.DeleteSense(entryId, previousSense.Id);
                return 1;
            },
            async (api, previousSense, currentSense) =>
            {
                var updateObjectInput = await SenseDiffToUpdate(previousSense, currentSense);
                if (updateObjectInput is not null) await api.UpdateSense(entryId, previousSense.Id, updateObjectInput);
                var changes = await ExampleSentenceSync(entryId,
                    previousSense.Id,
                    currentSense.ExampleSentences,
                    previousSense.ExampleSentences,
                    api);
                return changes + (updateObjectInput is null ? 0 : 1);
            });
    }

    private async Task<int> ExampleSentenceSync(Guid entryId,
        Guid senseId,
        IList<ExampleSentence> currentExampleSentences,
        IList<ExampleSentence> previousExampleSentences,
        ILexboxApi api)
    {
        return await DiffCollection(api,
            previousExampleSentences,
            currentExampleSentences,
            async (api, currentExampleSentence) =>
            {
                await api.CreateExampleSentence(entryId, senseId, currentExampleSentence);
                return 1;
            },
            async (api, previousExampleSentence) =>
            {
                await api.DeleteExampleSentence(entryId, senseId, previousExampleSentence.Id);
                return 1;
            },
            async (api, previousExampleSentence, currentExampleSentence) =>
            {
                var updateObjectInput = ExampleDiffToUpdate(previousExampleSentence, currentExampleSentence);
                if (updateObjectInput is null) return 0;
                await api.UpdateExampleSentence(entryId, senseId, previousExampleSentence.Id, updateObjectInput);
                return 1;
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
                return Task.FromResult(1);
            },
            (_, previousDomain) =>
            {
                patchDocument.Remove(sense => sense.SemanticDomains, previousSense.SemanticDomains.IndexOf(previousDomain));
                return Task.FromResult(1);
            },
            (_, previousDomain, currentDomain) =>
            {
                //do nothing, semantic domains are not editable here
                return Task.FromResult(0);
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

    private static async Task<int> DiffCollection<T>(
        ILexboxApi api,
        IList<T> previous,
        IList<T> current,
        Func<ILexboxApi, T, Task<int>> add,
        Func<ILexboxApi, T, Task<int>> remove,
        Func<ILexboxApi, T, T, Task<int>> replace) where T : IObjectWithId
    {
        var changes = 0;
        var currentEntriesDict = current.ToDictionary(entry => entry.Id);
        foreach (var previousEntry in previous)
        {
            if (currentEntriesDict.TryGetValue(previousEntry.Id, out var currentEntry))
            {
                changes += await replace(api, previousEntry, currentEntry);
            }
            else
            {
                changes += await remove(api, previousEntry);
            }

            currentEntriesDict.Remove(previousEntry.Id);
        }
        foreach (var value in currentEntriesDict.Values)
        {
            changes += await add(api, value);
        }
        return changes;
    }
}
