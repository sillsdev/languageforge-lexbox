using FwDataMiniLcmBridge.Api;
using LcmCrdt;
using MiniLcm.Models;

namespace FwLiteProjectSync;

public static class CrdtRepairs
{
#pragma warning disable CS0618 // Type or member is obsolete
    public static async Task<int> SyncMissingTranslationIds(Entry[] snapshotEntries, FwDataMiniLcmApi fwDataApi, CrdtMiniLcmApi crdtApi, bool dryRun = false)
    {
        using var activity = FwLiteProjectSyncActivitySource.Value.StartActivity();
        // Sync any available IDs from fwdata to the snapshot and the crdt entries
        // This should only need to be run once per project.
        var syncedIdCount = 0;
        var exampleSentenceIdToTranslationId = new Dictionary<Guid, Guid>();
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

                    if (snapshotTranslation.Id != Translation.MissingTranslationId)
                    {
                        // already has a valid ID
                        continue;
                    }

                    // Match the crdt API translation ID behaviour
                    // Change objects need to handle the Default ID rather than the MissingTranslationId,
                    // because the API returns the Default ID and thus needs to anticipate it being passed back in.
                    snapshotTranslation.Id = exampleSentence.DefaultFirstTranslationId;

                    var fwDataExampleSentence = await fwDataApi.GetExampleSentence(entry.Id, sense.Id, exampleSentence.Id);
                    if (fwDataExampleSentence is null)
                    {
                        // example sentence was deleted, so all translations will be deleted via cascade
                        continue;
                    }

                    var fwDataTranslation = fwDataExampleSentence.Translations.FirstOrDefault();
                    if (fwDataTranslation?.Id is null)
                    {
                        // fwdata translation was deleted.
                        // Using the default translation ID will probably result in the deletion syncing to the crdt translation.
                        // (it won't if the crdt translation with the default ID was deleted and a new one was created,
                        // in which case, it arguably shouldn't be deleted.)
                        continue;
                    }

                    var validTranslationId = fwDataTranslation.Id;
                    snapshotTranslation.Id = validTranslationId;

                    var crdtExampleSentence = await crdtApi.GetExampleSentence(entry.Id, sense.Id, exampleSentence.Id);
                    var crdtTranslation = crdtExampleSentence?.Translations.FirstOrDefault();
                    if (crdtTranslation is null)
                    {
                        // crdt translation was deleted.
                        // nothing to do. The deletion will sync to the fwdata translation.
                    }
                    else
                    {
                        // We assume we're overwriting Translation.MissingTranslationId/the Default ID.
                        // But there's a slight change we're not, because the translation could have been deleted and recreated with a new valid ID.
                        // However, until this "repair" we see crdt's as only having a single translation.
                        // I.e. the ID is essentially meaningless and semantically the user was actually just editing the synced/only fwdata translation.
                        syncedIdCount++;
                        exampleSentenceIdToTranslationId.Add(exampleSentence.Id, validTranslationId);
                    }
                }
            }
        }

        if (!dryRun && exampleSentenceIdToTranslationId.Any())
        {
            await crdtApi.SetFirstTranslationIds(exampleSentenceIdToTranslationId);
        }

        activity?.AddTag("Sync.CrdtRepairs.SyncedTranslationIds", syncedIdCount);
        return syncedIdCount;
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
