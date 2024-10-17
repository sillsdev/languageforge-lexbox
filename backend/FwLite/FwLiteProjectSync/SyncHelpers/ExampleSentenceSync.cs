using MiniLcm;
using MiniLcm.Models;
using SystemTextJsonPatch;

namespace FwLiteProjectSync.SyncHelpers;

public static class ExampleSentenceSync
{
    public static async Task<int> Sync(Guid entryId,
        Guid senseId,
        IList<ExampleSentence> afterExampleSentences,
        IList<ExampleSentence> beforeExampleSentences,
        IMiniLcmApi api)
    {
        Func<IMiniLcmApi, ExampleSentence, Task<int>> add = async (api, afterExampleSentence) =>
        {
            await api.CreateExampleSentence(entryId, senseId, afterExampleSentence);
            return 1;
        };
        Func<IMiniLcmApi, ExampleSentence, Task<int>> remove = async (api, beforeExampleSentence) =>
        {
            await api.DeleteExampleSentence(entryId, senseId, beforeExampleSentence.Id);
            return 1;
        };
        Func<IMiniLcmApi, ExampleSentence, ExampleSentence, Task<int>> replace =
            async (api, beforeExampleSentence, afterExampleSentence) =>
            {
                var updateObjectInput = DiffToUpdate(beforeExampleSentence, afterExampleSentence);
                if (updateObjectInput is null) return 0;
                await api.UpdateExampleSentence(entryId, senseId, beforeExampleSentence.Id, updateObjectInput);
                return 1;
            };
        return await DiffCollection.Diff(api,
            beforeExampleSentences,
            afterExampleSentences,
            add,
            remove,
            replace);
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
}
