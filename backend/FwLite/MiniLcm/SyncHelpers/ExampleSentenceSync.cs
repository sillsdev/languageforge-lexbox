using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class ExampleSentenceSync
{
    public static async Task<int> Sync(Guid entryId,
        Guid senseId,
        IList<ExampleSentence> beforeExampleSentences,
        IList<ExampleSentence> afterExampleSentences,
        IMiniLcmApi api,
        SyncContext context)
    {
        return await DiffCollection.DiffOrderable(
            beforeExampleSentences,
            afterExampleSentences,
            new ExampleSentencesDiffApi(api, entryId, senseId, context));
    }

    public static async Task<int> Sync(Guid entryId,
        Guid senseId,
        ExampleSentence beforeExampleSentence,
        ExampleSentence afterExampleSentence,
        IMiniLcmApi api)
    {
        var updateObjectInput = DiffToUpdate(beforeExampleSentence, afterExampleSentence);
        if (updateObjectInput is not null)
            await api.SubmitUpdateExampleSentence(entryId, senseId, beforeExampleSentence.Id, updateObjectInput);
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

    internal class ExampleSentencesDiffApi(IMiniLcmApi api, Guid entryId, Guid senseId, SyncContext context) : IOrderableCollectionDiffApi<ExampleSentence, Guid>
    {
        public Guid GetId(ExampleSentence value)
        {
            return value.Id;
        }

        public async Task<int> Add(ExampleSentence example, BetweenPosition<ExampleSentence> between)
        {
            var position = new BetweenPosition(between.Previous?.Id, between.Next?.Id);
            // a known id arriving here is a move; its new parent's Add owns it, then a three-way sync applies edits
            if (context.ExistedBefore(example) is { } before)
            {
                await api.MoveExampleSentenceToSense(entryId, senseId, example.Id, position);
                return 1 + await Sync(entryId, senseId, before, example, api);
            }
            await api.SubmitCreateExampleSentence(entryId, senseId, example, position);
            return 1;
        }

        public async Task<int> Move(ExampleSentence example, BetweenPosition<ExampleSentence> between)
        {
            // tolerant: the example may have been deleted on the side we're applying to, making the reorder moot
            await api.SubmitMoveExampleSentence(entryId, senseId, example.Id, new BetweenPosition(between.Previous?.Id, between.Next?.Id));
            return 1;
        }

        public async Task<int> Remove(ExampleSentence example)
        {
            // it was moved. Add will handle it.
            if (context.StillExists(example)) return 0;
            await api.DeleteExampleSentence(entryId, senseId, example.Id);
            return 1;
        }

        public Task<int> Replace(ExampleSentence beforeExampleSentence, ExampleSentence afterExampleSentence)
        {
            return Sync(entryId, senseId, beforeExampleSentence, afterExampleSentence, api);
        }
    }
}
