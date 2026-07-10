using Microsoft.Extensions.Logging;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace MiniLcm.Import;

/// <summary>
/// Populates <paramref name="importTo"/> from a <see cref="ProjectSnapshot"/> in dependency order
/// (writing systems → parts of speech → publications → complex-form types → variant types →
/// morph types → semantic domains → entries). Both FwData→CRDT import and template-based project
/// creation funnel through here, so the ordering and the create-vs-update rules live in exactly one place.
/// </summary>
public class ProjectImporter(ILogger<ProjectImporter> logger)
{
    public async Task ImportProject(IMiniLcmApi importTo, ProjectSnapshot snapshot)
    {
        logger.LogInformation("Starting project import");

        await ImportWritingSystems(importTo, snapshot.WritingSystems);

        logger.LogInformation("Importing {Count} parts of speech", snapshot.PartsOfSpeech.Length);
        foreach (var partOfSpeech in snapshot.PartsOfSpeech)
        {
            await importTo.CreatePartOfSpeech(partOfSpeech);
            logger.LogInformation("Imported part of speech {Id}", partOfSpeech.Id);
        }

        logger.LogInformation("Importing {Count} publications", snapshot.Publications.Length);
        foreach (var publication in snapshot.Publications)
        {
            await importTo.CreatePublication(publication);
            logger.LogInformation("Imported publication {Id}", publication.Id);
        }

        logger.LogInformation("Importing {Count} complex form types", snapshot.ComplexFormTypes.Length);
        foreach (var complexFormType in snapshot.ComplexFormTypes)
        {
            await importTo.CreateComplexFormType(complexFormType);
            logger.LogInformation("Imported complex form type {Id}", complexFormType.Id);
        }

        logger.LogInformation("Importing {Count} variant types", snapshot.VariantTypes.Length);
        foreach (var variantType in snapshot.VariantTypes)
        {
            await importTo.CreateVariantType(variantType);
            logger.LogInformation("Imported variant type {Id}", variantType.Id);
        }

        // Reconcile against the destination's existing morph types: MorphTypeSync updates them in place
        // and creates any missing, but rejects removals — canonical morph types can't be deleted.
        logger.LogInformation("Importing/Syncing {Count} morph types", snapshot.MorphTypes.Length);
        var existingMorphTypes = await importTo.GetMorphTypes().ToArrayAsync();
        await MorphTypeSync.Sync(existingMorphTypes, snapshot.MorphTypes, importTo);

        logger.LogInformation("Importing {Count} semantic domains", snapshot.SemanticDomains.Length);
        await importTo.BulkImportSemanticDomains(snapshot.SemanticDomains.ToAsyncEnumerable());

        logger.LogInformation("Importing {Count} entries", snapshot.Entries.Length);
        await importTo.BulkCreateEntries(snapshot.Entries.ToAsyncEnumerable());

        logger.LogInformation("Completed project import");
    }

    public async Task ImportWritingSystems(IMiniLcmApi importTo, WritingSystems writingSystems)
    {
        logger.LogInformation("Importing {Count} analysis writing systems", writingSystems.Analysis.Length);
        foreach (var ws in writingSystems.Analysis)
        {
            await importTo.CreateWritingSystem(ws);
            logger.LogInformation("Imported analysis writing system {WsId} - {Id}", ws.WsId, ws.MaybeId);
        }

        logger.LogInformation("Importing {Count} vernacular writing systems", writingSystems.Vernacular.Length);
        foreach (var ws in writingSystems.Vernacular)
        {
            await importTo.CreateWritingSystem(ws);
            logger.LogInformation("Imported vernacular writing system {WsId} - {Id}", ws.WsId, ws.MaybeId);
        }
    }
}
