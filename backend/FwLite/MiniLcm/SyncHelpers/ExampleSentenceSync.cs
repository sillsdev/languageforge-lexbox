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
        if (updateObjectInput is not null)
            await api.UpdateExampleSentence(entryId, senseId, beforeExampleSentence.Id, updateObjectInput);
        var translationChanges = await DiffCollection.Diff(beforeExampleSentence.Translations,
            afterExampleSentence.Translations,
            new TranslationDiffApi(api, entryId, senseId, beforeExampleSentence.Id));
        return (updateObjectInput is not null ? 1 : 0) + translationChanges;
    }

    public static UpdateObjectInput<ExampleSentence>? DiffToUpdate(ExampleSentence beforeExampleSentence,
        ExampleSentence afterExampleSentence)
    {
        JsonPatchDocument<ExampleSentence> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<ExampleSentence>(
            nameof(ExampleSentence.Sentence),
            beforeExampleSentence.Sentence,
            afterExampleSentence.Sentence));
        if (!Equals(beforeExampleSentence.Reference, afterExampleSentence.Reference))
        {
            patchDocument.Replace(exampleSentence => exampleSentence.Reference, afterExampleSentence.Reference);
        }

        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<ExampleSentence>(patchDocument);
    }

    public static UpdateObjectInput<Translation>? DiffToUpdate(Translation before, Translation after)
    {
        JsonPatchDocument<Translation> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Translation>(
            nameof(Translation.Text),
            before.Text,
            after.Text));
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<Translation>(patchDocument);
    }

    private class TranslationDiffApi(IMiniLcmApi api, Guid entryId, Guid senseId, Guid exampleId): CollectionDiffApi<Translation, Guid>
    {
        public override async Task<int> Add(Translation value)
        {
            await api.AddTranslation(entryId, senseId, exampleId, value);
            return 1;
        }

        public override async Task<int> Remove(Translation value)
        {
            await api.RemoveTranslation(entryId, senseId, exampleId, value.Id);
            return 1;
        }

        public override async Task<int> Replace(Translation before, Translation after)
        {
            var update = DiffToUpdate(before, after);
            if (update is null) return 0;
            await api.UpdateTranslation(entryId, senseId, exampleId, before.Id, update);
            return 1;
        }

        public override Guid GetId(Translation value)
        {
            return value.Id;
        }
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
