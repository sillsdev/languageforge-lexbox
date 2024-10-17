using MiniLcm;
using MiniLcm.Models;
using SystemTextJsonPatch;

namespace FwLiteProjectSync.SyncHelpers;

public static class ExampleSentenceSync
{
    public static async Task<int> Sync(Guid entryId,
        Guid senseId,
        IList<ExampleSentence> currentExampleSentences,
        IList<ExampleSentence> previousExampleSentences,
        IMiniLcmApi api)
    {
        Func<IMiniLcmApi, ExampleSentence, Task<int>> add = async (api, currentExampleSentence) =>
        {
            await api.CreateExampleSentence(entryId, senseId, currentExampleSentence);
            return 1;
        };
        Func<IMiniLcmApi, ExampleSentence, Task<int>> remove = async (api, previousExampleSentence) =>
        {
            await api.DeleteExampleSentence(entryId, senseId, previousExampleSentence.Id);
            return 1;
        };
        Func<IMiniLcmApi, ExampleSentence, ExampleSentence, Task<int>> replace =
            async (api, previousExampleSentence, currentExampleSentence) =>
            {
                var updateObjectInput = DiffToUpdate(previousExampleSentence, currentExampleSentence);
                if (updateObjectInput is null) return 0;
                await api.UpdateExampleSentence(entryId, senseId, previousExampleSentence.Id, updateObjectInput);
                return 1;
            };
        return await DiffCollection.Diff(api,
            previousExampleSentences,
            currentExampleSentences,
            add,
            remove,
            replace);
    }

    public static UpdateObjectInput<ExampleSentence>? DiffToUpdate(ExampleSentence previousExampleSentence,
        ExampleSentence currentExampleSentence)
    {
        JsonPatchDocument<ExampleSentence> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<ExampleSentence>(
            nameof(ExampleSentence.Sentence),
            previousExampleSentence.Sentence,
            currentExampleSentence.Sentence));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<ExampleSentence>(
            nameof(ExampleSentence.Translation),
            previousExampleSentence.Translation,
            currentExampleSentence.Translation));
        if (previousExampleSentence.Reference != currentExampleSentence.Reference)
        {
            patchDocument.Replace(exampleSentence => exampleSentence.Reference, currentExampleSentence.Reference);
        }

        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<ExampleSentence>(patchDocument);
    }
}
