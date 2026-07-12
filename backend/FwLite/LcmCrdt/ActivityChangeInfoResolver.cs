using System.Linq.Expressions;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Data;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;
using MiniLcm.Models;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Db;

namespace LcmCrdt;

/// <summary>
/// Batch-resolves a human label (and the root entry) for each change in a page of activity, so summaries
/// can name the entry/sense/vocab object a change is about without a per-row lookup. Degradable: leaves
/// <see cref="ActivityChangeInfo.Subject"/> null for types it doesn't resolve (the frontend falls back to a type label).
/// Reads the projected snapshot tables, so labels reflect the current state; objects missing from the
/// projection (deleted) are recovered from their latest snapshot so a delete can still name its subject.
/// The display headword is the best non-audio alternative across writing systems with morph-type markers
/// applied (e.g. "-ness" for a suffix); it's null when there's no displayable headword (all empty/audio-only)
/// or the entry is missing entirely, and the frontend renders a translatable placeholder in that case.
/// </summary>
internal static class ActivityChangeInfoResolver
{
    public static async Task<ProjectActivity[]> ResolveAsync(ICrdtDbContext db, IReadOnlyList<ProjectActivity> activities)
    {
        var entryIds = new HashSet<Guid>();
        var senseIds = new HashSet<Guid>();
        var exampleIds = new HashSet<Guid>();
        var componentIds = new HashSet<Guid>();
        var partOfSpeechIds = new HashSet<Guid>();
        var semanticDomainIds = new HashSet<Guid>();
        var publicationIds = new HashSet<Guid>();
        var complexFormTypeIds = new HashSet<Guid>();
        var morphTypeIds = new HashSet<Guid>();

        foreach (var change in activities.SelectMany(a => a.Changes))
        {
            switch (BucketFor(change.Change.EntityType))
            {
                case nameof(Entry): entryIds.Add(change.EntityId); break;
                case nameof(Sense): senseIds.Add(change.EntityId); break;
                case nameof(ExampleSentence): exampleIds.Add(change.EntityId); break;
                case nameof(ComplexFormComponent): componentIds.Add(change.EntityId); break;
                case nameof(PartOfSpeech): partOfSpeechIds.Add(change.EntityId); break;
                case nameof(SemanticDomain): semanticDomainIds.Add(change.EntityId); break;
                case nameof(Publication): publicationIds.Add(change.EntityId); break;
                case nameof(ComplexFormType): complexFormTypeIds.Add(change.EntityId); break;
                case nameof(MorphType): morphTypeIds.Add(change.EntityId); break;
            }

            // Some changes name a referenced item only by id; fold those ids into the same batch-loads so we can label them.
            switch (change.Change)
            {
                case SetPartOfSpeechChange { PartOfSpeechId: { } posId }: partOfSpeechIds.Add(posId); break;
                case RemoveSemanticDomainChange r: semanticDomainIds.Add(r.SemanticDomainId); break;
                case RemovePublicationChange r: publicationIds.Add(r.PublicationId); break;
                case RemoveComplexFormTypeChange r: complexFormTypeIds.Add(r.ComplexFormTypeId); break;
                case AddEntryComponentChange a:
                    entryIds.Add(a.ComponentEntryId);
                    // Also load the complex-form entry so we can name the subject when the CFC has been deleted
                    // (soft-deleted CFCs are absent from the projection dict, so the ComplexFormComponent case
                    // below falls back to this change's ComplexFormEntryId).
                    entryIds.Add(a.ComplexFormEntryId);
                    break;
                case SetComplexFormComponentChange { ComponentEntryId: { } cid }: entryIds.Add(cid); break;
            }
        }

        // Walk down to the root entry: component → entry, example → sense → entry. Load both ends of a component link so we can name either.
        var components = await LoadComponents(db, componentIds);
        foreach (var component in components.Values)
        {
            entryIds.Add(component.ComplexFormEntryId);
            entryIds.Add(component.ComponentEntryId);
        }

        // A deleted component link is absent from the projection above, so "Removed component" would have no
        // headwords. Recover its endpoints from its create change (AddEntryComponentChange carries both entry
        // ids) — enough to name the complex form and component for basic orientation.
        var deletedComponentIds = componentIds.Where(id => !components.ContainsKey(id)).ToHashSet();
        var deletedComponentEndpoints = await LoadComponentEndpointsFromCreate(db, deletedComponentIds);
        foreach (var (complexFormEntryId, componentEntryId) in deletedComponentEndpoints.Values)
        {
            entryIds.Add(complexFormEntryId);
            entryIds.Add(componentEntryId);
        }
        var examples = await LoadExamples(db, exampleIds);
        await RecoverDeleted(db, exampleIds, examples);
        foreach (var example in examples.Values) senseIds.Add(example.SenseId);
        var senses = await LoadSenses(db, senseIds);
        await RecoverDeleted(db, senseIds, senses);
        foreach (var sense in senses.Values) entryIds.Add(sense.EntryId);

        // Sibling senses of every affected sense's entry, so a sense's subject can carry its 1-based position
        // (and detect duplicate glosses) without a per-sense query. Ordered by Order to match the editor.
        var sensesByEntry = await LoadSensesByEntry(db, senses.Values.Select(s => s.EntryId).ToHashSet());

        var entries = await LoadEntries(db, entryIds);
        await RecoverDeleted(db, entryIds, entries);
        var partsOfSpeech = await LoadNamed<PartOfSpeech>(db, partOfSpeechIds, p => new PartOfSpeech { Id = p.Id, Name = p.Name });
        await RecoverDeleted(db, partOfSpeechIds, partsOfSpeech);
        var semanticDomains = await LoadSemanticDomains(db, semanticDomainIds);
        await RecoverDeleted(db, semanticDomainIds, semanticDomains);
        var publications = await LoadNamed<Publication>(db, publicationIds, p => new Publication { Id = p.Id, Name = p.Name });
        await RecoverDeleted(db, publicationIds, publications);
        var complexFormTypes = await LoadNamed<ComplexFormType>(db, complexFormTypeIds, c => new ComplexFormType { Id = c.Id, Name = c.Name });
        await RecoverDeleted(db, complexFormTypeIds, complexFormTypes);
        var morphTypes = await LoadNamed<MorphType>(db, morphTypeIds, m => new MorphType { Id = m.Id, Kind = m.Kind, Name = m.Name });
        await RecoverDeleted(db, morphTypeIds, morphTypes);

        // Markers (e.g. suffix "-") for the display headword, keyed by morph-type kind; first wins on duplicates.
        // Only needed to render an entry headword, so skip the load entirely when no entry is being resolved.
        var morphLookup = entryIds.Count == 0
            ? new Dictionary<MorphTypeKind, MorphType>()
            : (await db.Set<MorphType>().Select(m => new MorphType { Kind = m.Kind, Prefix = m.Prefix, Postfix = m.Postfix }).ToListAsyncLinqToDB())
                .GroupBy(m => m.Kind)
                .ToDictionary(g => g.Key, g => g.First());

        string? Headword(Guid entryId) => entries.TryGetValue(entryId, out var entry) ? DisplayHeadword(entry, morphLookup) : null;

        // The distinguishing label for a sense within its entry. FieldWorks numbers senses positionally and
        // shows the number when an entry has more than one sense — independent of the gloss (unlike homograph
        // numbers, which key off identical headwords). Rendered like a homograph number: >1 sense →
        // "{gloss}{subscript number}"; when the gloss is empty, "({number})" — parenthesized so a bare
        // position can't be mistaken for a gloss that IS a digit (a bare subscript would have nothing to
        // attach to, and this string is data, so a translatable "no gloss" placeholder can't live here).
        // A lone sense → its gloss, or "" when it has none (nothing to distinguish).
        string SenseGlossPart(Sense sense)
        {
            var siblings = sensesByEntry.GetValueOrDefault(sense.EntryId) ?? [];
            var index = siblings.FindIndex(s => s.Id == sense.Id);
            var glossText = Label(sense.Gloss);
            // A deleted sense (recovered from its snapshot) is absent from the live sibling list — it has no
            // meaningful position, so no number.
            if (index < 0) return glossText ?? "";
            var number = index + 1;
            var multiple = siblings.Count > 1;
            if (!string.IsNullOrEmpty(glossText)) return multiple ? glossText + Subscript(number) : glossText;
            return multiple ? $"({number})" : "";
        }

        // "headword › senseLabel". Degrades to just the headword when the sense has nothing to distinguish it
        // (a lone, gloss-less sense), and to null when there's neither a headword nor a distinguishing label
        // (Subject is display data, so a translatable fallback has to come from the frontend, not here).
        string? SenseLabel(string? headword, Sense sense)
        {
            var glossPart = SenseGlossPart(sense);
            if (headword is null) return string.IsNullOrEmpty(glossPart) ? null : glossPart;
            return string.IsNullOrEmpty(glossPart) ? headword : $"{headword} › {glossPart}";
        }

        return activities.Select(activity => activity with
        {
            ChangeInfo = activity.Changes.Select(change => Build(change, Headword)).ToList(),
        }).ToArray();

        ActivityChangeInfo Build(ChangeEntity<IChange> change, Func<Guid, string?> headword)
        {
            var info = ResolveReorder(change);
            if (info is null)
            {
                var (subject, rootEntryId) = ResolveSubject(change, headword);
                info = new ActivityChangeInfo(subject, rootEntryId, TargetLabel(change));
            }
            return info.RootEntryId is { } rootId ? info with { RootEntryHeadword = headword(rootId) } : info;
        }

        // A reorder reads best as "<container> · Reordered <item> <name>": the subject is the parent whose list changed, the target is the moved item.
        ActivityChangeInfo? ResolveReorder(ChangeEntity<IChange> change)
        {
            switch (change.Change)
            {
                case Changes.SetOrderChange<Sense> when senses.TryGetValue(change.EntityId, out var sense):
                    return new ActivityChangeInfo(Headword(sense.EntryId), sense.EntryId, SenseGlossPart(sense));
                case Changes.SetOrderChange<ExampleSentence> when examples.TryGetValue(change.EntityId, out var ex) && senses.TryGetValue(ex.SenseId, out var exSense):
                    return new ActivityChangeInfo(SenseLabel(Headword(exSense.EntryId), exSense), exSense.EntryId, null);
                case Changes.SetOrderChange<ComplexFormComponent> when components.TryGetValue(change.EntityId, out var comp):
                    return new ActivityChangeInfo(Headword(comp.ComplexFormEntryId), comp.ComplexFormEntryId,
                        entries.ContainsKey(comp.ComponentEntryId) ? Headword(comp.ComponentEntryId) : null);
                default:
                    return null;
            }
        }

        (string? Subject, Guid? RootEntryId) ResolveSubject(ChangeEntity<IChange> change, Func<Guid, string?> headword)
        {
            var id = change.EntityId;
            switch (BucketFor(change.Change.EntityType))
            {
                case nameof(Entry):
                    return (headword(id), id);
                case nameof(Sense) when senses.TryGetValue(id, out var sense):
                    // Create-sense reads as an entry-level change ("headword · Added sense senseN"), so the
                    // subject is the parent entry's headword and the sense identifier goes to Target.
                    // Sense edits keep the "headword › senseLabel" subject so field changes read as sense-level.
                    if (change.Change is CreateSenseChange)
                        return (headword(sense.EntryId), sense.EntryId);
                    return (SenseLabel(headword(sense.EntryId), sense), sense.EntryId);
                case nameof(ExampleSentence) when examples.TryGetValue(id, out var ex) && senses.TryGetValue(ex.SenseId, out var exSense):
                    return (SenseLabel(headword(exSense.EntryId), exSense), exSense.EntryId);
                case nameof(ComplexFormComponent) when components.TryGetValue(id, out var component):
                    return (headword(component.ComplexFormEntryId), component.ComplexFormEntryId);
                case nameof(ComplexFormComponent) when deletedComponentEndpoints.TryGetValue(id, out var ep):
                    // The CFC is deleted (absent from the projection); name the complex form from the endpoints
                    // recovered from its create change. Covers both "Removed component" and add-then-deleted.
                    return (headword(ep.ComplexFormEntryId), ep.ComplexFormEntryId);
                case nameof(PartOfSpeech) when partsOfSpeech.TryGetValue(id, out var pos):
                    return (Label(pos.Name), null);
                case nameof(SemanticDomain) when semanticDomains.TryGetValue(id, out var domain):
                    return (SemanticDomainLabel(domain), null);
                case nameof(Publication) when publications.TryGetValue(id, out var publication):
                    return (Label(publication.Name), null);
                case nameof(ComplexFormType) when complexFormTypes.TryGetValue(id, out var cft):
                    return (Label(cft.Name), null);
                case nameof(MorphType) when morphTypes.TryGetValue(id, out var morphType):
                    return (Label(morphType.Name), null);
                default:
                    return (null, null);
            }
        }

        // The item a change references only by id (the part of speech set, the domain/publication/type removed).
        string? TargetLabel(ChangeEntity<IChange> change) => change.Change switch
        {
            SetPartOfSpeechChange { PartOfSpeechId: { } id } when partsOfSpeech.TryGetValue(id, out var pos) => Label(pos.Name),
            RemoveSemanticDomainChange r when semanticDomains.TryGetValue(r.SemanticDomainId, out var domain) => SemanticDomainLabel(domain),
            RemovePublicationChange r when publications.TryGetValue(r.PublicationId, out var publication) => Label(publication.Name),
            RemoveComplexFormTypeChange r when complexFormTypes.TryGetValue(r.ComplexFormTypeId, out var cft) => Label(cft.Name),
            // Component links resolve the component being linked (the change's subject is the complex form).
            AddEntryComponentChange a when entries.TryGetValue(a.ComponentEntryId, out var component) => DisplayHeadword(component, morphLookup),
            SetComplexFormComponentChange { ComponentEntryId: { } cid } when entries.TryGetValue(cid, out var component) => DisplayHeadword(component, morphLookup),
            // A deleted component link (e.g. "Removed component"): name the component from the recovered endpoints.
            _ when change.Change.EntityType == typeof(ComplexFormComponent)
                   && deletedComponentEndpoints.TryGetValue(change.EntityId, out var ep)
                   && entries.TryGetValue(ep.ComponentEntryId, out var component) => DisplayHeadword(component, morphLookup),
            // Sense-create pairs with the ResolveSubject override above: subject is the parent entry headword,
            // target names the new sense ("gwa₁ · Added sense senseN" — SenseGlossPart falls back to a subscript
            // when the gloss is empty, matching sense-edit summaries).
            CreateSenseChange when senses.TryGetValue(change.EntityId, out var sense) => SenseGlossPart(sense),
            _ => null
        };
    }

    private static string BucketFor(Type entityType) => entityType.Name;

    // Deleted objects are dropped from the projected tables, so the loads below can't label them ("Deleted
    // word" with no word). Recover any still-missing ids from their latest snapshot — a delete's own snapshot
    // keeps every field, only DeletedAt is set. No-op in the common case where nothing is missing; when ids
    // ARE missing (deleted objects visible on the page — rare and few), one Take(1) query per id beats
    // loading each entity's whole snapshot history.
    private static async Task RecoverDeleted<T>(ICrdtDbContext db, HashSet<Guid> ids, Dictionary<Guid, T> loaded)
        where T : class, IObjectWithId
    {
        foreach (var id in ids.Where(id => !loaded.ContainsKey(id)))
        {
            var snapshot = await (
                from commit in db.Commits.DefaultOrderDescending()
                from s in db.Snapshots.InnerJoin(s => s.CommitId == commit.Id)
                where s.EntityId == id
                select s
            ).FirstOrDefaultAsyncLinqToDB();
            if (snapshot?.Entity.DbObject is T entity) loaded[id] = entity;
        }
    }

    // Per-type projected loads: only the columns the resolver reads, so we skip hydrating heavy JSONB columns
    // (definitions, notes, nested senses/components, pictures, …) that never feed a label.

    private static async Task<Dictionary<Guid, Entry>> LoadEntries(ICrdtDbContext db, HashSet<Guid> ids)
    {
        if (ids.Count == 0) return [];
        var loaded = await db.Set<Entry>().Where(e => ids.Contains(e.Id))
            .Select(e => new Entry
            {
                Id = e.Id,
                CitationForm = e.CitationForm,
                LexemeForm = e.LexemeForm,
                MorphType = e.MorphType,
                HomographNumber = e.HomographNumber,
            })
            .ToListAsyncLinqToDB();
        return loaded.ToDictionary(e => e.Id);
    }

    private static async Task<Dictionary<Guid, Sense>> LoadSenses(ICrdtDbContext db, HashSet<Guid> ids)
    {
        if (ids.Count == 0) return [];
        var loaded = await db.Set<Sense>().Where(s => ids.Contains(s.Id))
            .Select(s => new Sense { Id = s.Id, EntryId = s.EntryId, Gloss = s.Gloss, Order = s.Order })
            .ToListAsyncLinqToDB();
        return loaded.ToDictionary(s => s.Id);
    }

    private static async Task<Dictionary<Guid, List<Sense>>> LoadSensesByEntry(ICrdtDbContext db, HashSet<Guid> entryIds)
    {
        if (entryIds.Count == 0) return [];
        var loaded = await db.Set<Sense>().Where(s => entryIds.Contains(s.EntryId))
            .Select(s => new Sense { Id = s.Id, EntryId = s.EntryId, Gloss = s.Gloss, Order = s.Order })
            .ToListAsyncLinqToDB();
        return loaded
            .GroupBy(s => s.EntryId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Order).ThenBy(s => s.Id).ToList());
    }

    private static async Task<Dictionary<Guid, ExampleSentence>> LoadExamples(ICrdtDbContext db, HashSet<Guid> ids)
    {
        if (ids.Count == 0) return [];
        var loaded = await db.Set<ExampleSentence>().Where(e => ids.Contains(e.Id))
            .Select(e => new ExampleSentence { Id = e.Id, SenseId = e.SenseId })
            .ToListAsyncLinqToDB();
        return loaded.ToDictionary(e => e.Id);
    }

    // The endpoints of deleted component links, recovered from their create change (which carries both entry
    // ids). Two steps: load the changes for these CFC ids, then read the ids off the AddEntryComponentChange.
    private static async Task<Dictionary<Guid, (Guid ComplexFormEntryId, Guid ComponentEntryId)>> LoadComponentEndpointsFromCreate(
        ICrdtDbContext db, HashSet<Guid> cfcIds)
    {
        if (cfcIds.Count == 0) return [];
        var changeEntities = await db.Set<ChangeEntity<IChange>>()
            .Where(ce => cfcIds.Contains(ce.EntityId)
                && Sql.Expr<string>("json_extract({0}, '$.\"$type\"')", ce.Change) == nameof(AddEntryComponentChange))
            .ToListAsyncLinqToDB();
        return changeEntities
            .Select(ce => ce.Change)
            .OfType<AddEntryComponentChange>()
            .GroupBy(c => c.EntityId)
            .ToDictionary(g => g.Key, g => (g.First().ComplexFormEntryId, g.First().ComponentEntryId));
    }

    private static async Task<Dictionary<Guid, ComplexFormComponent>> LoadComponents(ICrdtDbContext db, HashSet<Guid> ids)
    {
        if (ids.Count == 0) return [];
        var loaded = await db.Set<ComplexFormComponent>().Where(c => ids.Contains(c.Id))
            .Select(c => new ComplexFormComponent
            {
                Id = c.Id,
                ComplexFormEntryId = c.ComplexFormEntryId,
                ComponentEntryId = c.ComponentEntryId,
            })
            .ToListAsyncLinqToDB();
        return loaded.ToDictionary(c => c.Id);
    }

    private static async Task<Dictionary<Guid, SemanticDomain>> LoadSemanticDomains(ICrdtDbContext db, HashSet<Guid> ids)
    {
        if (ids.Count == 0) return [];
        var loaded = await db.Set<SemanticDomain>().Where(d => ids.Contains(d.Id))
            .Select(d => new SemanticDomain { Id = d.Id, Code = d.Code, Name = d.Name })
            .ToListAsyncLinqToDB();
        return loaded.ToDictionary(d => d.Id);
    }

    // Vocab objects the resolver only labels by name (part of speech, publication, complex-form type, morph type).
    private static async Task<Dictionary<Guid, T>> LoadNamed<T>(ICrdtDbContext db, HashSet<Guid> ids, Expression<Func<T, T>> project)
        where T : class, IObjectWithId
    {
        if (ids.Count == 0) return [];
        var loaded = await db.Set<T>().Where(o => ids.Contains(o.Id)).Select(project).ToListAsyncLinqToDB();
        return loaded.ToDictionary(o => o.Id);
    }

    // Order by writing-system code so a multi-writing-system name resolves to the same alternative every time, matching Entry.Headword().
    private static string? Label(MultiString multiString) =>
        multiString.Values.OrderBy(kvp => kvp.Key.Code).Select(kvp => kvp.Value?.Trim()).FirstOrDefault(text => !string.IsNullOrEmpty(text));

    // Best non-audio alternative across writing systems (ordered by code, matching Label()) with morph-type markers
    // applied (e.g. "-ness" for a suffix); null when there's no displayable text. FieldWorks distinguishes same-spelled
    // entries by a homograph number shown as a subscript, assigned (> 0) only on a collision.
    private static string? DisplayHeadword(Entry entry, IReadOnlyDictionary<MorphTypeKind, MorphType> morphLookup)
    {
        var headword = EntryQueryHelpers.ComputeHeadwords(entry, morphLookup).Values
            .Where(kvp => !kvp.Key.IsAudio)
            .OrderBy(kvp => kvp.Key.Code)
            .Select(kvp => kvp.Value?.Trim())
            .FirstOrDefault(text => !string.IsNullOrEmpty(text));
        if (string.IsNullOrEmpty(headword)) return null;
        return entry.HomographNumber > 0 ? headword + Subscript(entry.HomographNumber) : headword;
    }

    private static string Subscript(int number) =>
        new(number.ToString().Select(digit => (char)('₀' + (digit - '0'))).ToArray());

    // The app shows a domain as "code name" (e.g. "5.2 Food"); the code alone is too cryptic to identify it.
    private static string SemanticDomainLabel(SemanticDomain domain) =>
        Label(domain.Name) is { } name ? $"{domain.Code} {name}" : domain.Code;
}
