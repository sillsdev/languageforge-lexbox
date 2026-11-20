using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteProjectSync.Pipeline;

/// <summary>
/// The concrete steps that make up a CRDT &lt;-&gt; FwData sync. Each entity is split into two directional
/// steps — one that writes the CRDT side (fwdata -&gt; crdt) and one that writes the FwData side
/// (crdt -&gt; fwdata) — so a step can be inserted before or after either direction. The CRDT direction of
/// every entity runs before entries' CRDT direction, and likewise for the FwData direction, because
/// entries reference the other entities. Entries additionally run in two phases: phase 1 (create/update/
/// delete without complex forms) then a dynamically-emitted phase-2 step that wires up complex forms and
/// components once every entry exists on the target side.
/// </summary>
public static class SyncSteps
{
    /// <summary>Which side a directional step writes to.</summary>
    public enum Direction
    {
        /// <summary>fwdata -&gt; crdt (writes the CRDT side).</summary>
        ToCrdt,
        /// <summary>crdt -&gt; fwdata (writes the FwData side).</summary>
        ToFwdata,
    }

    /// <summary>The step name used as an ordering key, e.g. <c>"Entry.ToCrdt"</c>. Insert around a phase by referencing it.</summary>
    public static string StepName<TEntity>(Direction direction) => $"{typeof(TEntity).Name}.{direction}";

    private static readonly string EntryToCrdt = StepName<Entry>(Direction.ToCrdt);
    private static readonly string EntryToFwdata = StepName<Entry>(Direction.ToFwdata);
    private const string EntryToCrdtComplexForms = "Entry.ToCrdt.ComplexForms";
    private const string EntryToFwdataComplexForms = "Entry.ToFwdata.ComplexForms";

    public static IReadOnlyList<SyncStep> CreateInitialSteps() =>
    [
        ToCrdt<WritingSystem>(async ctx =>
            await WritingSystemSync.Sync(ctx.Snapshot.WritingSystems, await ctx.FwdataApi.GetWritingSystems(), ctx.CrdtApi)),
        ToFwdata<WritingSystem>(async ctx =>
            await WritingSystemSync.Sync(await ctx.FwdataApi.GetWritingSystems(), await ctx.CrdtApi.GetWritingSystems(), ctx.FwdataApi)),

        ToCrdt<Publication>(async ctx =>
            await PublicationSync.Sync(ctx.Snapshot.Publications, await ctx.FwdataApi.GetPublications().ToArrayAsync(), ctx.CrdtApi)),
        ToFwdata<Publication>(async ctx =>
            await PublicationSync.Sync(await ctx.FwdataApi.GetPublications().ToArrayAsync(), await ctx.CrdtApi.GetPublications().ToArrayAsync(), ctx.FwdataApi)),

        ToCrdt<PartOfSpeech>(async ctx =>
            await PartOfSpeechSync.Sync(ctx.Snapshot.PartsOfSpeech, await ctx.FwdataApi.GetPartsOfSpeech().ToArrayAsync(), ctx.CrdtApi)),
        ToFwdata<PartOfSpeech>(async ctx =>
            await PartOfSpeechSync.Sync(await ctx.FwdataApi.GetPartsOfSpeech().ToArrayAsync(), await ctx.CrdtApi.GetPartsOfSpeech().ToArrayAsync(), ctx.FwdataApi)),

        ToCrdt<SemanticDomain>(async ctx =>
            await SemanticDomainSync.Sync(ctx.Snapshot.SemanticDomains, await ctx.FwdataApi.GetSemanticDomains().ToArrayAsync(), ctx.CrdtApi)),
        ToFwdata<SemanticDomain>(async ctx =>
            await SemanticDomainSync.Sync(await ctx.FwdataApi.GetSemanticDomains().ToArrayAsync(), await ctx.CrdtApi.GetSemanticDomains().ToArrayAsync(), ctx.FwdataApi)),

        ToCrdt<ComplexFormType>(async ctx =>
            await ComplexFormTypeSync.Sync(ctx.Snapshot.ComplexFormTypes, await ctx.FwdataApi.GetComplexFormTypes().ToArrayAsync(), ctx.CrdtApi)),
        ToFwdata<ComplexFormType>(async ctx =>
            await ComplexFormTypeSync.Sync(await ctx.FwdataApi.GetComplexFormTypes().ToArrayAsync(), await ctx.CrdtApi.GetComplexFormTypes().ToArrayAsync(), ctx.FwdataApi)),

        ToCrdt<MorphType>(async ctx =>
            await MorphTypeSync.Sync(ctx.Snapshot.MorphTypes, await ctx.FwdataApi.GetMorphTypes().ToArrayAsync(), ctx.CrdtApi)),
        ToFwdata<MorphType>(async ctx =>
            await MorphTypeSync.Sync(await ctx.FwdataApi.GetMorphTypes().ToArrayAsync(), await ctx.CrdtApi.GetMorphTypes().ToArrayAsync(), ctx.FwdataApi)),

        EntriesToCrdt(),
        EntriesToFwdata(),
    ];

    /// <summary>A CRDT-direction step for a non-entry entity. Runs before entries' CRDT direction.</summary>
    private static SyncStepBuilder ToCrdt<TEntity>(Func<SyncStepContext, Task<int>> sync) =>
        SyncStep.Named(StepName<TEntity>(Direction.ToCrdt), async ctx => ctx.RecordCrdtChanges(await sync(ctx)))
            .Before(EntryToCrdt);

    /// <summary>A FwData-direction step for a non-entity entity. Runs after its own CRDT direction and before entries' FwData direction.</summary>
    private static SyncStepBuilder ToFwdata<TEntity>(Func<SyncStepContext, Task<int>> sync) =>
        SyncStep.Named(StepName<TEntity>(Direction.ToFwdata), async ctx => ctx.RecordFwdataChanges(await sync(ctx)))
            .After(StepName<TEntity>(Direction.ToCrdt))
            .Before(EntryToFwdata);

    /// <summary>
    /// Entries CRDT direction, phase 1 (without complex forms). Emits the phase-2 complex-forms step for
    /// exactly the entries it just created. Runs after every non-entry CRDT-direction step (declared by those).
    /// </summary>
    private static SyncStep EntriesToCrdt() =>
        SyncStep.Named(EntryToCrdt, async ctx =>
        {
            var currentFw = await ctx.FwdataApi.GetAllEntries().ToArrayAsync();
            var (changes, added) = await EntrySync.SyncWithoutComplexFormsAndComponents(ctx.Snapshot.Entries, currentFw, ctx.CrdtApi);
            ctx.RecordCrdtChanges(changes);
            ctx.AddStep(EntriesToCrdtComplexFormsStep(ctx.Snapshot.Entries, currentFw, added));
        });

    /// <summary>
    /// Entries FwData direction, phase 1 (without complex forms). Runs after the entries CRDT direction (so
    /// the crdt entries exist to diff against) and after every non-entry FwData-direction step (declared by those).
    /// </summary>
    private static SyncStep EntriesToFwdata() =>
        SyncStep.Named(EntryToFwdata, async ctx =>
        {
            var currentFw = await ctx.FwdataApi.GetAllEntries().ToArrayAsync();
            var currentCrdt = await ctx.CrdtApi.GetAllEntries().ToArrayAsync();
            var (changes, added) = await EntrySync.SyncWithoutComplexFormsAndComponents(currentFw, currentCrdt, ctx.FwdataApi);
            ctx.RecordFwdataChanges(changes);
            ctx.AddStep(EntriesToFwdataComplexFormsStep(currentFw, added));
        }).After(EntryToCrdt);

    /// <summary>Phase 2 for the CRDT side: wire complex forms/components against the (captured) fwdata truth.</summary>
    private static SyncStep EntriesToCrdtComplexFormsStep(Entry[] snapshotEntries, Entry[] currentFw, ICollection<Entry> added) =>
        SyncStep.Named(EntryToCrdtComplexForms, async ctx =>
        {
            Entry[] before = [.. Updated(snapshotEntries, currentFw), .. added];
            ctx.RecordCrdtChanges(await EntrySync.SyncComplexFormsAndComponents(before, currentFw, ctx.CrdtApi));
        }).After(EntryToCrdt);

    /// <summary>
    /// Phase 2 for the FwData side. Runs after the CRDT-side complex-forms step and re-reads crdt so it sees
    /// the freshly-wired complex forms as the target truth.
    /// </summary>
    private static SyncStep EntriesToFwdataComplexFormsStep(Entry[] currentFw, ICollection<Entry> added) =>
        SyncStep.Named(EntryToFwdataComplexForms, async ctx =>
        {
            var currentCrdt = await ctx.CrdtApi.GetAllEntries().ToArrayAsync();
            Entry[] before = [.. Updated(currentFw, currentCrdt), .. added];
            ctx.RecordFwdataChanges(await EntrySync.SyncComplexFormsAndComponents(before, currentCrdt, ctx.FwdataApi));
        }).After(EntryToCrdtComplexForms);

    /// <summary>The 'before' entries that still exist in 'after', matching EntrySync.SyncFull's phase-2 input.</summary>
    private static IEnumerable<Entry> Updated(Entry[] before, Entry[] after) =>
        before.Where(b => after.Any(a => a.Id == b.Id));
}
