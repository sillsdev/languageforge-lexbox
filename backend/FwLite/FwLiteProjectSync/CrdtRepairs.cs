using FwDataMiniLcmBridge.Api;
using LcmCrdt;
using MiniLcm.Models;

namespace FwLiteProjectSync;

public static class CrdtRepairs
{
    public static async Task<int> SyncMissingTranslationIds(Entry[] snapshotEntries, FwDataMiniLcmApi fwDataApi, CrdtMiniLcmApi crdtApi, bool dryRun)
    {
        // Step 1: Sync any available IDs from fwdata to the snapshot and the crdt entries
        // This step should only need to be run once per project
        // Looking up fwdata example-sentences one-by-one might be slow,
        // but after the first run there should be no more to do, so future runs will be fast.
        // We don't want to diff the entire entry, sense and example-sentence collections every time.
        var syncedIdCount = 0;
        foreach (var entry in snapshotEntries)
        {
            foreach (var sense in entry.Senses)
            {
                foreach (var exampleSentence in sense.ExampleSentences)
                {
                    var snapshotTranslation = exampleSentence.Translations.FirstOrDefault();
                    if (snapshotTranslation is null)
                    {
                        // if fwdata has a translation then it was just added and will safely sync to crdt with a valid ID
                        continue;
                    }

                    if (snapshotTranslation.Id != Translation.DefaultFirstTranslationId)
                    {
                        // already has a valid ID
                        continue;
                    }

                    var fwDataExampleSentence = await fwDataApi.GetExampleSentence(entry.Id, sense.Id, exampleSentence.Id);
                    if (fwDataExampleSentence is null)
                    {
                        // example sentence was deleted, so all translations will be deleted via cascade
                        continue;
                    }

                    var fwDataTranslation = fwDataExampleSentence.Translations.FirstOrDefault();
                    var validTranslationId = fwDataTranslation?.Id
                        // If there's no fwdata translation/ID then it was deleted.
                        // If we don't update the ID here, then only the crdt ID will be updated in the loop below.
                        // That would put the snapshot and crdt out of sync and break the delete.
                        ?? Guid.NewGuid();

                    if (!dryRun)
                    {
                        await crdtApi.SetFirstTranslationId(exampleSentence.Id, validTranslationId);
                        snapshotTranslation.Id = validTranslationId;
                    }
                    syncedIdCount++;
                }
            }
        }

        // Step 2: Any remaining translations were added in the crdt project, so they can be given a new valid ID that will be synced back to fwdata
        // This step needs to be run until we've seen all crdt translations that were created without the model change
        await foreach (var entry in crdtApi.GetAllEntries())
        {
            foreach (var sense in entry.Senses)
            {
                foreach (var exampleSentence in sense.ExampleSentences)
                {
                    var firstTranslation = exampleSentence.Translations.FirstOrDefault();
                    if (firstTranslation is null) continue;
                    if (firstTranslation.Id != Translation.DefaultFirstTranslationId) continue;
                    if (!dryRun)
                    {
                        var newId = Guid.NewGuid();
                        await crdtApi.SetFirstTranslationId(exampleSentence.Id, newId);
                    }
                    syncedIdCount++;
                }
            }
        }
        return syncedIdCount;
    }
}
