using MiniLcm;
using MiniLcm.Models;
using SystemTextJsonPatch;

namespace FwLiteProjectSync.SyncHelpers;

public static class SenseSync
{
    public static async Task<int> Sync(Guid entryId,
        Sense currentSense,
        Sense previousSense,
        IMiniLcmApi api)
    {
        var updateObjectInput = await SenseDiffToUpdate(previousSense, currentSense);
        if (updateObjectInput is not null) await api.UpdateSense(entryId, previousSense.Id, updateObjectInput);
        var changes = await ExampleSentenceSync.Sync(entryId,
            previousSense.Id,
            currentSense.ExampleSentences,
            previousSense.ExampleSentences,
            api);
        return changes + (updateObjectInput is null ? 0 : 1);
    }

    public static async Task<UpdateObjectInput<Sense>?> SenseDiffToUpdate(Sense previousSense, Sense currentSense)
    {
        JsonPatchDocument<Sense> patchDocument = new();
        patchDocument.Operations.AddRange(
            MultiStringDiff.GetMultiStringDiff<Sense>(nameof(Sense.Gloss), previousSense.Gloss, currentSense.Gloss));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Sense>(nameof(Sense.Definition),
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

        await DiffCollection.Diff(null!,
            previousSense.SemanticDomains,
            currentSense.SemanticDomains,
            (_, domain) =>
            {
                patchDocument.Add(sense => sense.SemanticDomains, domain);
                return Task.FromResult(1);
            },
            (_, previousDomain) =>
            {
                patchDocument.Remove(sense => sense.SemanticDomains,
                    previousSense.SemanticDomains.IndexOf(previousDomain));
                return Task.FromResult(1);
            },
            (_, previousDomain, currentDomain) =>
            {
                //do nothing, semantic domains are not editable here
                return Task.FromResult(0);
            });
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<Sense>(patchDocument);
    }
}
