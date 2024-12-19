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
        return await DiffCollection.Diff(
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
        if (beforeExampleSentence.Reference != afterExampleSentence.Reference)
        {
            patchDocument.Replace(exampleSentence => exampleSentence.Reference, afterExampleSentence.Reference);
        }

        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<ExampleSentence>(patchDocument);
    }

    private class ExampleSentencesDiffApi(IMiniLcmApi api, Guid entryId, Guid senseId) : ObjectWithIdCollectionDiffApi<ExampleSentence>
    {
        public override async Task<int> Add(ExampleSentence afterExampleSentence)
        {
            await api.CreateExampleSentence(entryId, senseId, afterExampleSentence);
            return 1;
        }

        public override async Task<int> Remove(ExampleSentence beforeExampleSentence)
        {
            await api.DeleteExampleSentence(entryId, senseId, beforeExampleSentence.Id);
            return 1;
        }

        public override Task<int> Replace(ExampleSentence beforeExampleSentence, ExampleSentence afterExampleSentence)
        {
            return Sync(entryId, senseId, beforeExampleSentence, afterExampleSentence, api);
        }
    }
}
