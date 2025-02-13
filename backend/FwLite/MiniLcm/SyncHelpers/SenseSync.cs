using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class SenseSync
{
    public static async Task<int> Sync(Guid entryId,
        Sense beforeSense,
        Sense afterSense,
        IMiniLcmApi api)
    {
        var updateObjectInput = SenseDiffToUpdate(beforeSense, afterSense);
        if (updateObjectInput is not null) await api.UpdateSense(entryId, beforeSense.Id, updateObjectInput);
        var changes = await ExampleSentenceSync.Sync(entryId,
            beforeSense.Id,
            beforeSense.ExampleSentences,
            afterSense.ExampleSentences,
            api);
        changes += await DiffCollection.Diff(
            beforeSense.SemanticDomains,
            afterSense.SemanticDomains,
            new SenseSemanticDomainsDiffApi(api, beforeSense.Id));
        return changes + (updateObjectInput is null ? 0 : 1);
    }

    public static UpdateObjectInput<Sense>? SenseDiffToUpdate(Sense beforeSense, Sense afterSense)
    {
        JsonPatchDocument<Sense> patchDocument = new();
        patchDocument.Operations.AddRange(
            MultiStringDiff.GetMultiStringDiff<Sense>(nameof(Sense.Gloss), beforeSense.Gloss, afterSense.Gloss));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Sense>(nameof(Sense.Definition),
            beforeSense.Definition,
            afterSense.Definition));

        if (beforeSense.PartOfSpeechId != afterSense.PartOfSpeechId)
        {
            patchDocument.Replace(sense => sense.PartOfSpeechId, afterSense.PartOfSpeechId);
        }

        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<Sense>(patchDocument);
    }

    private class SenseSemanticDomainsDiffApi(IMiniLcmApi api, Guid senseId) : ObjectWithIdCollectionDiffApi<SemanticDomain>
    {
        public override async Task<int> Add(SemanticDomain domain)
        {
            await api.AddSemanticDomainToSense(senseId, domain);
            return 1;
        }

        public override async Task<int> Remove(SemanticDomain beforeDomain)
        {
            await api.RemoveSemanticDomainFromSense(senseId, beforeDomain.Id);
            return 1;
        }

        public override Task<int> Replace(SemanticDomain previousSemDom, SemanticDomain currentSemDom)
        {
            return Task.FromResult(0);
        }
    }
}
