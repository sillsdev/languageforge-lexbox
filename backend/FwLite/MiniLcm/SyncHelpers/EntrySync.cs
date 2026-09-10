using MiniLcm.Exceptions;
using MiniLcm.Models;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace MiniLcm.SyncHelpers;

public static class EntrySync
{
    public static async Task<int> SyncFull(Entry[] beforeEntries,
        Entry[] afterEntries,
        IMiniLcmApi api)
    {
        var (changes, addedEntries) = await SyncWithoutComplexFormsAndComponents(beforeEntries, afterEntries, api);
        var updatedBeforeEntries = beforeEntries.Where(before => afterEntries.Any(after => after.Id == before.Id));
        changes += await SyncComplexFormsAndComponents([.. updatedBeforeEntries, .. addedEntries], afterEntries, api);
        return changes;
    }

    public static async Task<(int Changes, ICollection<Entry> Added)> SyncWithoutComplexFormsAndComponents(Entry[] beforeEntries,
        Entry[] afterEntries,
        IMiniLcmApi api)
    {
        var context = SyncContext.For(beforeEntries, afterEntries);
        var (changes, added) = await DiffCollection.DiffAndGetAdded(beforeEntries, afterEntries, new EntriesDiffApi(api, context));
        changes += await context.DeleteAll();
        return (changes, added);
    }

    /// <summary>
    /// Syncs only the complex forms and components of the before and after entries.
    /// <exception cref="InvalidOperationException">When the before and after entries do not match.</exception>
    /// </summary>
    public static async Task<int> SyncComplexFormsAndComponents(Entry[] beforeEntries,
        Entry[] afterEntries,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(beforeEntries, afterEntries,
            new ObjectWithIdCollectionReplaceDiffApi<Entry>(
                (before, after) => SyncComplexFormsAndComponents(before, after, api)));
    }

    /// <summary>
    /// The viewer's updateEntry path. Re-parenting a picture or translation between this entry's own senses
    /// is refused with <see cref="MoveNotSupportedException"/> and nothing is written (develop did delete+create).
    /// </summary>
    public static async Task<int> SyncFull(Entry beforeEntry, Entry afterEntry, IMiniLcmApi api)
    {
        var context = SyncContext.For(beforeEntry, afterEntry);
        var changes = await SyncWithoutComplexFormsAndComponents(beforeEntry, afterEntry, api, context);
        changes += await context.DeleteAll();
        changes += await SyncComplexFormsAndComponents(beforeEntry, afterEntry, api);
        return changes;
    }

    private static async Task<int> SyncWithoutComplexFormsAndComponents(Entry beforeEntry, Entry afterEntry, IMiniLcmApi api, SyncContext context)
    {
        try
        {
            var updateObjectInput = EntryDiffToUpdate(beforeEntry, afterEntry);
            if (updateObjectInput is not null) await api.SubmitUpdateEntry(afterEntry.Id, updateObjectInput);
            var changes = await SensesSync(afterEntry.Id, beforeEntry.Senses, afterEntry.Senses, api, context);
            changes += await Sync(afterEntry.Id, beforeEntry.ComplexFormTypes, afterEntry.ComplexFormTypes, api);
            changes += await SyncPublications(afterEntry.Id, beforeEntry.PublishIn, afterEntry.PublishIn, api);
            return changes + (updateObjectInput is null ? 0 : 1);
        }
        catch (Exception e)
        {
            throw new SyncObjectException($"Failed to sync entry {afterEntry.Id}", e);
        }
    }

    public static async Task<int> SyncComplexFormsAndComponents(Entry beforeEntry, Entry afterEntry, IMiniLcmApi api)
    {
        try
        {
            var changes = 0;
            changes += await SyncComplexFormComponents(beforeEntry.Components, afterEntry.Components, api);
            changes += await SyncComplexForms(beforeEntry.ComplexForms, afterEntry.ComplexForms, api);
            return changes;
        }
        catch (Exception e)
        {
            throw new SyncObjectException($"Failed to sync complex forms and components of entry {afterEntry.Id}", e);
        }
    }

    private static async Task<int> SyncPublications(Guid entryId,
        IList<Publication> beforePublications,
        IList<Publication> afterPublications,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforePublications,
            afterPublications,
            new PublicationsDiffApi(api, entryId));
    }

    private static async Task<int> Sync(Guid entryId,
        IList<ComplexFormType> beforeComplexFormTypes,
        IList<ComplexFormType> afterComplexFormTypes,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforeComplexFormTypes,
            afterComplexFormTypes,
            new ComplexFormTypesDiffApi(api, entryId));
    }

    private static async Task<int> SyncComplexFormComponents(IList<ComplexFormComponent> beforeComponents, IList<ComplexFormComponent> afterComponents, IMiniLcmApi api)
    {
        return await DiffCollection.DiffOrderable(
            beforeComponents,
            afterComponents,
            new ComplexFormComponentsDiffApi(api)
        );
    }

    private static async Task<int> SyncComplexForms(IList<ComplexFormComponent> beforeComponents, IList<ComplexFormComponent> afterComponents, IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforeComponents,
            afterComponents,
            new ComplexFormsDiffApi(api)
        );
    }

    private static async Task<int> SensesSync(Guid entryId,
        IList<Sense> beforeSenses,
        IList<Sense> afterSenses,
        IMiniLcmApi api,
        SyncContext context)
    {
        return await DiffCollection.DiffOrderable(beforeSenses, afterSenses, new SensesDiffApi(api, entryId, context));
    }

    public static UpdateObjectInput<Entry>? EntryDiffToUpdate(Entry beforeEntry, Entry afterEntry)
    {
        JsonPatchDocument<Entry> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Entry>(nameof(Entry.LexemeForm), beforeEntry.LexemeForm, afterEntry.LexemeForm));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Entry>(nameof(Entry.CitationForm), beforeEntry.CitationForm, afterEntry.CitationForm));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Entry>(nameof(Entry.Note), beforeEntry.Note, afterEntry.Note));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Entry>(nameof(Entry.LiteralMeaning), beforeEntry.LiteralMeaning, afterEntry.LiteralMeaning));
        if (beforeEntry.MorphType != afterEntry.MorphType)
            patchDocument.Operations.Add(new Operation<Entry>("replace", $"/{nameof(Entry.MorphType)}", null, afterEntry.MorphType));
        if (beforeEntry.HomographNumber != afterEntry.HomographNumber)
            patchDocument.Operations.Add(new Operation<Entry>("replace", $"/{nameof(Entry.HomographNumber)}", null, afterEntry.HomographNumber));
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<Entry>(patchDocument);
    }

    internal class EntriesDiffApi(IMiniLcmApi api, SyncContext context) : ObjectWithIdCollectionDiffApi<Entry>
    {
        public override async Task<(int, Entry)> AddAndGet(Entry afterEntry)
        {
            // a moved-in descendant still lives under its old parent, so it can't ride along in the create;
            // create the entry without it, then let the recursive sync move it in.
            if (!context.HasMovedInDescendants(afterEntry))
                return (1, await api.CreateEntry(afterEntry, CreateEntryOptions.WithoutComplexFormsAndComponents));
            var payload = context.WithoutMovedInDescendants(afterEntry);
            var created = await api.CreateEntry(payload, CreateEntryOptions.WithoutComplexFormsAndComponents);
            var changes = 1 + await SyncWithoutComplexFormsAndComponents(payload, afterEntry, api, context);
            return (changes, created with { Senses = afterEntry.Senses });
        }

        // entries never move, but their delete defers so a sense can be moved out before the cascade
        public override Task<int> Remove(Entry entry) => context.DeferDelete(() => DeleteEntry(entry));

        private async Task<int> DeleteEntry(Entry entry)
        {
            await api.DeleteEntry(entry.Id);
            return 1;
        }

        public override Task<int> Replace(Entry before, Entry after)
        {
            return SyncWithoutComplexFormsAndComponents(before, after, api, context);
        }
    }

    private class ComplexFormTypesDiffApi(IMiniLcmApi api, Guid entryId) : ObjectWithIdCollectionDiffApi<ComplexFormType>
    {
        public override async Task<int> Add(ComplexFormType afterComplexFormType)
        {
            await api.AddComplexFormType(entryId, afterComplexFormType.Id);
            return 1;
        }

        public override async Task<int> Remove(ComplexFormType beforeComplexFormType)
        {
            await api.RemoveComplexFormType(entryId, beforeComplexFormType.Id);
            return 1;
        }

        public override Task<int> Replace(ComplexFormType before, ComplexFormType after)
        {
            return Task.FromResult(0);
        }
    }

    private class PublicationsDiffApi(IMiniLcmApi api, Guid entryId) : ObjectWithIdCollectionDiffApi<Publication>
    {
        public override async Task<int> Add(Publication afterPub)
        {
            await api.AddPublication(entryId, afterPub.Id);
            return 1;
        }

        public override async Task<int> Remove(Publication beforePub)
        {
            await api.RemovePublication(entryId, beforePub.Id);
            return 1;
        }

        public override Task<int> Replace(Publication beforePub, Publication afterPub)
        {
            return Task.FromResult(0);
        }
    }

    private class ComplexFormsDiffApi(IMiniLcmApi api) : CollectionDiffApi<ComplexFormComponent, (Guid, Guid, Guid?)>
    {
        public override (Guid, Guid, Guid?) GetId(ComplexFormComponent component)
        {
            //we can't use the ID as there's none defined by Fw so it won't work as a sync key
            return (component.ComplexFormEntryId, component.ComponentEntryId, component.ComponentSenseId);
        }

        public override async Task<int> Add(ComplexFormComponent after)
        {
            await api.SubmitCreateComplexFormComponent(after);
            return 1;
        }

        public override async Task<int> Remove(ComplexFormComponent before)
        {
            await api.DeleteComplexFormComponent(before);
            return 1;
        }

        public override Task<int> Replace(ComplexFormComponent beforeComponent, ComplexFormComponent afterComponent)
        {
            if (beforeComponent.ComplexFormEntryId == afterComponent.ComplexFormEntryId &&
                beforeComponent.ComponentEntryId == afterComponent.ComponentEntryId &&
                beforeComponent.ComponentSenseId == afterComponent.ComponentSenseId)
            {
                return Task.FromResult(0);
            }
            throw new InvalidOperationException($"changing complex form components is not supported, they should just be deleted and recreated");
        }
    }

    private class ComplexFormComponentsDiffApi(IMiniLcmApi api) : IOrderableCollectionDiffApi<ComplexFormComponent, (Guid, Guid, Guid?)>
    {
        public (Guid, Guid, Guid?) GetId(ComplexFormComponent component)
        {
            // we can't use the ID as there's none defined by Fw so it won't work as a sync key
            return (component.ComplexFormEntryId, component.ComponentEntryId, component.ComponentSenseId);
        }

        public async Task<int> Add(ComplexFormComponent after, BetweenPosition<ComplexFormComponent> between)
        {
            await api.SubmitCreateComplexFormComponent(after, between);
            return 1;
        }

        public async Task<int> Move(ComplexFormComponent component, BetweenPosition<ComplexFormComponent> between)
        {
            await api.SubmitMoveComplexFormComponent(component, between);
            return 1;
        }

        public async Task<int> Remove(ComplexFormComponent before)
        {
            await api.DeleteComplexFormComponent(before);
            return 1;
        }

        public Task<int> Replace(ComplexFormComponent beforeComponent, ComplexFormComponent afterComponent)
        {
            if (beforeComponent.ComplexFormEntryId == afterComponent.ComplexFormEntryId &&
                beforeComponent.ComponentEntryId == afterComponent.ComponentEntryId &&
                beforeComponent.ComponentSenseId == afterComponent.ComponentSenseId)
            {
                return Task.FromResult(0);
            }
            throw new InvalidOperationException($"changing complex form components is not supported, they should just be deleted and recreated");
        }
    }

    internal class SensesDiffApi(IMiniLcmApi api, Guid entryId, SyncContext context) : IOrderableCollectionDiffApi<Sense, Guid>
    {
        public Guid GetId(Sense sense)
        {
            return sense.Id;
        }

        public async Task<int> Add(Sense sense, BetweenPosition<Sense> between)
        {
            var position = new BetweenPosition(between.Previous?.Id, between.Next?.Id);
            // a known id arriving here is a move; its new parent's Add owns it, then a three-way sync applies edits
            if (context.MovedIn(sense) is { } before)
            {
                await api.MoveSenseToEntry(entryId, sense.Id, position);
                return 1 + await SenseSync.Sync(entryId, before, sense, api, context);
            }
            // a genuinely new sense whose payload holds a moved-in example: create it without the example, then move it in
            if (context.HasMovedInDescendants(sense))
            {
                var payload = context.WithoutMovedInDescendants(sense);
                await api.SubmitCreateSense(entryId, payload, position);
                return 1 + await SenseSync.Sync(entryId, payload, sense, api, context);
            }
            await api.SubmitCreateSense(entryId, sense, position);
            return 1;
        }

        public async Task<int> Move(Sense sense, BetweenPosition<Sense> between)
        {
            // tolerant: the sense may have been deleted on the side we're applying to, making the reorder moot
            await api.SubmitMoveSense(entryId, sense.Id, new BetweenPosition(between.Previous?.Id, between.Next?.Id));
            return 1;
        }

        public Task<int> Remove(Sense sense)
        {
            // still exists elsewhere after => it moved out; its new parent's Add owns the move, nothing to delete here
            return context.StillExists(sense) ? Task.FromResult(0) : context.DeferDelete(() => DeleteSense(sense));
        }

        private async Task<int> DeleteSense(Sense sense)
        {
            await api.DeleteSense(entryId, sense.Id);
            return 1;
        }

        public Task<int> Replace(Sense before, Sense after)
        {
            return SenseSync.Sync(entryId, before, after, api, context);
        }
    }
}
