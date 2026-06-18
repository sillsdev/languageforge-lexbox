using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace MiniLcm.Import;

/// <summary>
/// Populates <paramref name="importTo"/> from a <see cref="ProjectSnapshot"/> in dependency order
/// (writing systems → parts of speech → publications → complex-form types → morph types →
/// semantic domains → entries). Both FwData→CRDT import and template-based project creation funnel
/// through here, so the ordering and the create-vs-update rules live in exactly one place.
/// </summary>
public static class ProjectImporter
{
    public static async Task ImportData(IMiniLcmApi importTo, ProjectSnapshot snapshot)
    {
        await ImportWritingSystems(importTo, snapshot.WritingSystems);

        foreach (var partOfSpeech in snapshot.PartsOfSpeech)
            await importTo.CreatePartOfSpeech(partOfSpeech);

        foreach (var publication in snapshot.Publications)
            await importTo.CreatePublication(publication);

        foreach (var complexFormType in snapshot.ComplexFormTypes)
            await importTo.CreateComplexFormType(complexFormType);

        // Reconcile against the destination's existing morph types: MorphTypeSync updates them in place
        // and creates any missing, but rejects removals — canonical morph types can't be deleted.
        var existingMorphTypes = await importTo.GetMorphTypes().ToArrayAsync();
        await MorphTypeSync.Sync(existingMorphTypes, snapshot.MorphTypes, importTo);

        await importTo.BulkImportSemanticDomains(snapshot.SemanticDomains.ToAsyncEnumerable());
        await importTo.BulkCreateEntries(snapshot.Entries.ToAsyncEnumerable());
    }

    public static async Task ImportWritingSystems(IMiniLcmApi importTo, WritingSystems writingSystems)
    {
        foreach (var ws in writingSystems.Analysis)
            await importTo.CreateWritingSystem(ws);
        foreach (var ws in writingSystems.Vernacular)
            await importTo.CreateWritingSystem(ws);
    }
}
