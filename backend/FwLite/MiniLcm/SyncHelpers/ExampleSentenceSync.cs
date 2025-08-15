using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class ExampleSentenceSync
{
    public static async Task<int> Sync(Guid entryId,
        Guid senseId,
        IList<ExampleSentence> beforeExampleSentences,
        IList<ExampleSentence> afterExampleSentences,
        IMiniLcmApi api)
    {
        return await DiffCollection.DiffOrderable(
            beforeExampleSentences,
            afterExampleSentences,
            new ExampleSentencesDiffApi(api, entryId, senseId));
    }

    public static async Task<int> Sync(Guid entryId,
        Guid senseId,
        ExampleSentence beforeExampleSentence,
        ExampleSentence afterExampleSentence,
        IMiniLcmApi api)
    {
        var updateObjectInput = DiffToUpdate(beforeExampleSentence, afterExampleSentence);
        if (updateObjectInput is null) return 0;
        await api.UpdateExampleSentence(entryId, senseId, beforeExampleSentence.Id, updateObjectInput);
        return 1;
    }

    public static UpdateObjectInput<ExampleSentence>? DiffToUpdate(ExampleSentence beforeExampleSentence,
        ExampleSentence afterExampleSentence)
    {
        JsonPatchDocument<ExampleSentence> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<ExampleSentence>(
            nameof(ExampleSentence.Sentence),
            beforeExampleSentence.Sentence,
            afterExampleSentence.Sentence));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<ExampleSentence>(
            nameof(ExampleSentence.Translation),
            beforeExampleSentence.Translation,
            afterExampleSentence.Translation));
        if (!Equals(beforeExampleSentence.Reference, afterExampleSentence.Reference))
        {
            patchDocument.Replace(exampleSentence => exampleSentence.Reference, afterExampleSentence.Reference);
        }

        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<ExampleSentence>(patchDocument);
    }

    private class ExampleSentencesDiffApi(IMiniLcmApi api, Guid entryId, Guid senseId) : IOrderableCollectionDiffApi<ExampleSentence>
    {
        public async Task<int> Add(ExampleSentence afterExampleSentence, BetweenPosition between)
        {
            await api.CreateExampleSentence(entryId, senseId, afterExampleSentence, between);
            return 1;
        }

        public async Task<int> Move(ExampleSentence example, BetweenPosition between)
        {
            await api.MoveExampleSentence(entryId, senseId, example.Id, between);
            return 1;
        }

        public async Task<int> Remove(ExampleSentence beforeExampleSentence)
        {
            await api.DeleteExampleSentence(entryId, senseId, beforeExampleSentence.Id);
            return 1;
        }

        public Task<int> Replace(ExampleSentence beforeExampleSentence, ExampleSentence afterExampleSentence)
        {
            return Sync(entryId, senseId, beforeExampleSentence, afterExampleSentence, api);
        }
    }
}
