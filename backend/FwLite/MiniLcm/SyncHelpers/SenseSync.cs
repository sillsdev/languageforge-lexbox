using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class SenseSync
{
    public static async Task<int> Sync(Guid entryId,
        Sense afterSense,
        Sense beforeSense,
        IMiniLcmApi api)
    {
        var updateObjectInput = await SenseDiffToUpdate(beforeSense, afterSense);
        if (updateObjectInput is not null) await api.UpdateSense(entryId, beforeSense.Id, updateObjectInput);
        var changes = await ExampleSentenceSync.Sync(entryId,
            beforeSense.Id,
            afterSense.ExampleSentences,
            beforeSense.ExampleSentences,
            api);
        changes += await DiffCollection.Diff(api,
            beforeSense.SemanticDomains,
            afterSense.SemanticDomains,
            async (api, domain) =>
            {
                await api.AddSemanticDomainToSense(beforeSense.Id, domain);
                return 1;
            },
            async (api, beforeDomain) =>
            {
                await api.RemoveSemanticDomainFromSense(beforeSense.Id, beforeDomain.Id);
                return 1;
            },
            (_, beforeDomain, afterDomain) =>
            {
                //do nothing, semantic domains are not editable here
                return Task.FromResult(0);
            });
        return changes + (updateObjectInput is null ? 0 : 1);
    }

    public static async Task<UpdateObjectInput<Sense>?> SenseDiffToUpdate(Sense beforeSense, Sense afterSense)
    {
        JsonPatchDocument<Sense> patchDocument = new();
        patchDocument.Operations.AddRange(
            MultiStringDiff.GetMultiStringDiff<Sense>(nameof(Sense.Gloss), beforeSense.Gloss, afterSense.Gloss));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Sense>(nameof(Sense.Definition),
            beforeSense.Definition,
            afterSense.Definition));
        if (beforeSense.PartOfSpeech != afterSense.PartOfSpeech)
        {
            patchDocument.Replace(sense => sense.PartOfSpeech, afterSense.PartOfSpeech);
        }

        if (beforeSense.PartOfSpeechId != afterSense.PartOfSpeechId)
        {
            patchDocument.Replace(sense => sense.PartOfSpeechId, afterSense.PartOfSpeechId);
        }

        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<Sense>(patchDocument);
    }
}
