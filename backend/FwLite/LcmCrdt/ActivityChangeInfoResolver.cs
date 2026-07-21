using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Data;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
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
    public static async Task<ProjectActivity[]> ResolveAsync(ICrdtDbContext db, IReadOnlyList<ProjectActivity> activities, WritingSystems writingSystems)
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
        var writingSystemIds = new HashSet<Guid>();
        var customViewIds = new HashSet<Guid>();
        var commentThreadIds = new HashSet<Guid>();
        var userCommentIds = new HashSet<Guid>();

        foreach (var change in activities.SelectMany(a => a.Changes))
        {
            switch (change.Change.EntityType.Name)
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
                case nameof(WritingSystem): writingSystemIds.Add(change.EntityId); break;
                case nameof(CustomView): customViewIds.Add(change.EntityId); break;
                case nameof(CommentThread): commentThreadIds.Add(change.EntityId); break;
                case nameof(UserComment): userCommentIds.Add(change.EntityId); break;
            }

            // Some changes name a referenced item only by id; fold those ids into the same batch-loads so we can label them.
            switch (change.Change)
            {
                case SetPartOfSpeechChange { PartOfSpeechId: { } posId }: partOfSpeechIds.Add(posId); break;
                case AddSemanticDomainChange a: semanticDomainIds.Add(a.SemanticDomain.Id); break;
                case RemoveSemanticDomainChange r: semanticDomainIds.Add(r.SemanticDomainId); break;
                case AddPublicationChange a: publicationIds.Add(a.Publication.Id); break;
                case RemovePublicationChange r: publicationIds.Add(r.PublicationId); break;
                case AddComplexFormTypeChange a: complexFormTypeIds.Add(a.ComplexFormType.Id); break;
                case RemoveComplexFormTypeChange r: complexFormTypeIds.Add(r.ComplexFormTypeId); break;
                case AddEntryComponentChange a:
                    entryIds.Add(a.ComponentEntryId);
                    entryIds.Add(a.ComplexFormEntryId);
                    break;
                case SetComplexFormComponentChange { ComponentEntryId: { } cid }: entryIds.Add(cid); break;
            }
        }

        // Comments resolve to the entry/sense/example their thread is attached to (subject) plus a snippet of the
        // comment text (target), mirroring example sentences. Load comments and their threads up front so the
        // commented subject's id can join the entry/sense/example batch-loads below (via the cascade further down).
        var userComments = await LoadWithRecovery(db, userCommentIds, LoadUserComments);
        foreach (var comment in userComments.Values) commentThreadIds.Add(comment.CommentThreadId);
        var commentThreads = await LoadWithRecovery(db, commentThreadIds, LoadCommentThreads);
        foreach (var thread in commentThreads.Values)
        {
            switch (thread.SubjectType)
            {
                case SubjectType.Entry: entryIds.Add(thread.SubjectId); break;
                case SubjectType.Sense: senseIds.Add(thread.SubjectId); break;
                case SubjectType.ExampleSentence: exampleIds.Add(thread.SubjectId); break;
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
        var examples = await LoadWithRecovery(db, exampleIds, LoadExamples);
        foreach (var example in examples.Values) senseIds.Add(example.SenseId);
        var senses = await LoadWithRecovery(db, senseIds, LoadSenses);
        foreach (var sense in senses.Values) entryIds.Add(sense.EntryId);

        // Sibling senses of every affected sense's entry, so a sense's subject can carry its 1-based position
        // (and detect duplicate glosses) without a per-sense query. Ordered by Order to match the editor.
        var sensesByEntry = await LoadSensesByEntry(db, [.. senses.Values.Select(s => s.EntryId)]);

        var entries = await LoadWithRecovery(db, entryIds, LoadEntries);
        var partsOfSpeech = await LoadNamed<PartOfSpeech>(db, partOfSpeechIds, p => new PartOfSpeech { Id = p.Id, Name = p.Name });
        var semanticDomains = await LoadWithRecovery(db, semanticDomainIds, LoadSemanticDomains);
        var publications = await LoadNamed<Publication>(db, publicationIds, p => new Publication { Id = p.Id, Name = p.Name });
        var complexFormTypes = await LoadNamed<ComplexFormType>(db, complexFormTypeIds, c => new ComplexFormType { Id = c.Id, Name = c.Name });
        var morphTypes = await LoadNamed<MorphType>(db, morphTypeIds, m => new MorphType { Id = m.Id, Kind = m.Kind, Name = m.Name });
        var writingSystemsById = await LoadNamed<WritingSystem>(db, writingSystemIds,
            w => new WritingSystem { Id = w.Id, WsId = w.WsId, Name = w.Name, Abbreviation = w.Abbreviation, Font = w.Font, Type = w.Type });
        var customViews = await LoadNamed<CustomView>(db, customViewIds, v => new CustomView { Id = v.Id, Name = v.Name });

        // Markers (e.g. suffix "-") for the display headword, keyed by morph-type kind; first wins on duplicates.
        // Only needed to render an entry headword, so skip the load entirely when no entry is being resolved.
        var morphLookup = entryIds.Count == 0
            ? []
            : (await db.Set<MorphType>().Select(m => new MorphType { Kind = m.Kind, Prefix = m.Prefix, Postfix = m.Postfix }).ToListAsyncLinqToDB())
                .GroupBy(m => m.Kind)
                .ToDictionary(g => g.Key, g => g.First());

        // The project's writing-system display order; the label helpers below pick the first non-empty
        // alternative in this order rather than alphabetically by writing-system code.
        var writingSystemOrder = BuildWsOrder(writingSystems);

        // The alternative to show for a multi-writing-system name: first non-empty in configured order (see BestAlternative).
        string? Label(MultiString multiString) => BestAlternative(multiString.Values, writingSystemOrder, v => v?.Trim());

        // Best non-audio headword alternative (configured order, like Label) with morph-type markers applied
        // (e.g. "-ness" for a suffix); null when there's no displayable text. FieldWorks distinguishes same-spelled
        // entries by a homograph number shown as a subscript, assigned (> 0) only on a collision.
        string? DisplayHeadword(Entry entry)
        {
            var headword = BestAlternative(
                EntryQueryHelpers.ComputeHeadwords(entry, morphLookup).Values.Where(kvp => !kvp.Key.IsAudio),
                writingSystemOrder,
                v => v?.Trim());
            if (string.IsNullOrEmpty(headword)) return null;
            return entry.HomographNumber > 0 ? headword + Subscript(entry.HomographNumber) : headword;
        }

        // The app shows a domain as "code name" (e.g. "5.2 Food"); the code alone is too cryptic to identify it.
        string SemanticDomainLabel(SemanticDomain domain) =>
            Label(domain.Name) is { } name ? $"{domain.Code} {name}" : domain.Code;

        string? Headword(Guid entryId) => entries.TryGetValue(entryId, out var entry) ? DisplayHeadword(entry) : null;

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

        return [.. activities.Select(activity => activity with
        {
            ChangeInfo = [.. activity.Changes.Select(change => Build(change, Headword))],
        })];

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
                    return new ActivityChangeInfo(SenseLabel(Headword(exSense.EntryId), exSense), exSense.EntryId, ExampleSnippet(ex.Sentence, writingSystemOrder));
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
            switch (change.Change.EntityType.Name)
            {
                case nameof(Entry):
                    return (headword(id), id);
                case nameof(Sense) when senses.TryGetValue(id, out var sense):
                    // Adding or removing a sense is a change to its parent entry's structure, so both read as
                    // entry-level ("headword · Added sense senseN" / "· Removed sense senseN"): the subject is the
                    // parent entry's headword and the sense identifier goes to Target. As inverse operations they
                    // stay mirror images. Field edits keep the "headword › senseLabel" subject so they read as
                    // sense-level (the sense itself is what's constant, a field is what changed).
                    if (change.Change is CreateSenseChange or DeleteChange<Sense>)
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
                // Writing systems and custom views name themselves by their plain (non-multi-string) display name.
                case nameof(WritingSystem) when writingSystemsById.TryGetValue(id, out var ws):
                    return (NullIfEmpty(ws.Name), null);
                case nameof(CustomView) when customViews.TryGetValue(id, out var view):
                    return (NullIfEmpty(view.Name), null);
                case nameof(CommentThread) when commentThreads.TryGetValue(id, out var thread):
                    return CommentSubject(thread, headword);
                case nameof(UserComment) when userComments.TryGetValue(id, out var comment)
                                              && commentThreads.TryGetValue(comment.CommentThreadId, out var commentThread):
                    return CommentSubject(commentThread, headword);
                default:
                    return (null, null);
            }
        }

        // A comment resolves to whatever its thread is attached to — the entry headword, the "headword › gloss"
        // sense label, or the example's sense label — with that entity's root entry. Mirrors the
        // Entry/Sense/ExampleSentence arms above so a comment reads at the same level as an edit to its subject.
        (string? Subject, Guid? RootEntryId) CommentSubject(CommentThread thread, Func<Guid, string?> headword)
        {
            var subjectId = thread.SubjectId;
            switch (thread.SubjectType)
            {
                case SubjectType.Entry:
                    return (headword(subjectId), subjectId);
                case SubjectType.Sense when senses.TryGetValue(subjectId, out var sense):
                    return (SenseLabel(headword(sense.EntryId), sense), sense.EntryId);
                case SubjectType.ExampleSentence when examples.TryGetValue(subjectId, out var ex) && senses.TryGetValue(ex.SenseId, out var exSense):
                    return (SenseLabel(headword(exSense.EntryId), exSense), exSense.EntryId);
                default:
                    return (null, null);
            }
        }

        // The item a change references only by id (the part of speech set, the domain/publication/type removed).
        string? TargetLabel(ChangeEntity<IChange> change) => change.Change switch
        {
            SetPartOfSpeechChange { PartOfSpeechId: { } id } when partsOfSpeech.TryGetValue(id, out var pos) => Label(pos.Name),
            // Add/remove of a reference names the referenced item as the target; the two stay mirror images.
            AddSemanticDomainChange a when semanticDomains.TryGetValue(a.SemanticDomain.Id, out var domain) => SemanticDomainLabel(domain),
            RemoveSemanticDomainChange r when semanticDomains.TryGetValue(r.SemanticDomainId, out var domain) => SemanticDomainLabel(domain),
            AddPublicationChange a when publications.TryGetValue(a.Publication.Id, out var publication) => Label(publication.Name),
            RemovePublicationChange r when publications.TryGetValue(r.PublicationId, out var publication) => Label(publication.Name),
            AddComplexFormTypeChange a when complexFormTypes.TryGetValue(a.ComplexFormType.Id, out var cft) => Label(cft.Name),
            RemoveComplexFormTypeChange r when complexFormTypes.TryGetValue(r.ComplexFormTypeId, out var cft) => Label(cft.Name),
            // Component links resolve the component being linked (the change's subject is the complex form).
            AddEntryComponentChange a when entries.TryGetValue(a.ComponentEntryId, out var component) => DisplayHeadword(component),
            SetComplexFormComponentChange { ComponentEntryId: { } cid } when entries.TryGetValue(cid, out var component) => DisplayHeadword(component),
            // A deleted component link (e.g. "Removed component"): name the component from the recovered endpoints.
            _ when change.Change.EntityType == typeof(ComplexFormComponent)
                   && deletedComponentEndpoints.TryGetValue(change.EntityId, out var ep)
                   && entries.TryGetValue(ep.ComponentEntryId, out var component) => DisplayHeadword(component),
            // Sense create/delete pair with the ResolveSubject override above: subject is the parent entry
            // headword, target names the sense ("gwa₁ · Added sense senseN" — SenseGlossPart falls back to a
            // subscript when the gloss is empty, matching sense-edit summaries).
            CreateSenseChange or DeleteChange<Sense> when senses.TryGetValue(change.EntityId, out var sense) => SenseGlossPart(sense),
            // Any change to an example sentence (create/edit/delete) names the sentence itself as the target: a
            // short truncated snippet, since the full text is too long and is the example's only identity.
            _ when change.Change.EntityType == typeof(ExampleSentence)
                   && examples.TryGetValue(change.EntityId, out var ex) => ExampleSnippet(ex.Sentence, writingSystemOrder),
            // Any change to a comment (create/edit/delete) names the comment text as the target: a short
            // truncated snippet, like an example sentence (the subject already names what it's a comment on).
            _ when change.Change.EntityType == typeof(UserComment)
                   && userComments.TryGetValue(change.EntityId, out var comment) => CommentSnippet(comment.Text),
            _ => null
        };
    }

    // A projected load paired with its deleted-id recovery — the two always go together for a label load, so a
    // deleted object can still be named. (LoadNamed recovers internally; the loads that recover a different way,
    // like ComplexFormComponent via its create change, don't use this.)
    private static async Task<Dictionary<Guid, T>> LoadWithRecovery<T>(ICrdtDbContext db,
        HashSet<Guid> ids,
        Func<ICrdtDbContext, HashSet<Guid>, Task<Dictionary<Guid, T>>> load)
        where T : class, IObjectWithId
    {
        var loaded = await load(db, ids);
        await RecoverDeleted(db, ids, loaded);
        return loaded;
    }

    // Deleted objects are dropped from the projected tables, so the loads below can't label them ("Deleted
    // word" with no word). Recover any still-missing ids from their latest snapshot — a delete's own snapshot
    // keeps every field, only DeletedAt is set. No-op in the common case where nothing is missing; when ids
    // ARE missing (deleted objects visible on the page — rare and few), one window query picks each entity's
    // newest snapshot without pulling its whole history.
    private static async Task RecoverDeleted<T>(ICrdtDbContext db, HashSet<Guid> ids, Dictionary<Guid, T> loaded)
        where T : class, IObjectWithId
    {
        var missingIds = ids.Where(id => !loaded.ContainsKey(id)).ToHashSet();
        if (missingIds.Count == 0) return;

        var snapshots = await (
            from row in
                from commit in db.Commits
                from s in db.Snapshots.InnerJoin(s => s.CommitId == commit.Id)
                where missingIds.Contains(s.EntityId)
                select new
                {
                    s,
                    // Same keys as Harmony's Commit.DefaultOrderDescending; must stay in sync (a window's
                    // Over() can't take that IQueryable extension, so the ordering is spelled out here).
                    rn = Sql.Ext.RowNumber().Over()
                    .PartitionBy(s.EntityId)
                    .OrderByDesc(commit.HybridDateTime.DateTime)
                    .ThenByDesc(commit.HybridDateTime.Counter)
                    .ThenByDesc(commit.Id).ToValue()
                }
            where row.rn == 1
            select row.s
        ).ToArrayAsyncLinqToDB();

        foreach (var snapshot in snapshots)
        {
            if (snapshot.Entity.DbObject is T entity) loaded[snapshot.EntityId] = entity;
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
            .Select(e => new ExampleSentence { Id = e.Id, SenseId = e.SenseId, Sentence = e.Sentence })
            .ToListAsyncLinqToDB();
        return loaded.ToDictionary(e => e.Id);
    }

    private static async Task<Dictionary<Guid, UserComment>> LoadUserComments(ICrdtDbContext db, HashSet<Guid> ids)
    {
        if (ids.Count == 0) return [];
        var loaded = await db.Set<UserComment>().Where(c => ids.Contains(c.Id))
            .Select(c => new UserComment { Id = c.Id, CommentThreadId = c.CommentThreadId, Text = c.Text })
            .ToListAsyncLinqToDB();
        return loaded.ToDictionary(c => c.Id);
    }

    // Only the thread's subject (what it's attached to) is needed to label a comment; its comment list isn't.
    private static async Task<Dictionary<Guid, CommentThread>> LoadCommentThreads(ICrdtDbContext db, HashSet<Guid> ids)
    {
        if (ids.Count == 0) return [];
        var loaded = await db.Set<CommentThread>().Where(t => ids.Contains(t.Id))
            .Select(t => new CommentThread { Id = t.Id, SubjectId = t.SubjectId, SubjectType = t.SubjectType })
            .ToListAsyncLinqToDB();
        return loaded.ToDictionary(t => t.Id);
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
    // Recovers deleted ids like the per-type loads, so a removed vocab object can still be named.
    private static async Task<Dictionary<Guid, T>> LoadNamed<T>(ICrdtDbContext db, HashSet<Guid> ids, Expression<Func<T, T>> project)
        where T : class, IObjectWithId
    {
        if (ids.Count == 0) return [];
        var loaded = await db.Set<T>().Where(o => ids.Contains(o.Id)).Select(project).ToListAsyncLinqToDB();
        var result = loaded.ToDictionary(o => o.Id);
        await RecoverDeleted(db, ids, result);
        return result;
    }

    // A plain (non-multi-string) display name, degraded to null when blank so it matches the null-when-empty
    // convention the MultiString labels use (the frontend renders a placeholder rather than an empty subject).
    private static string? NullIfEmpty(string? name) => string.IsNullOrWhiteSpace(name) ? null : name;

    private static string Subscript(int number)
    {
        Span<char> buffer = stackalloc char[11]; // widest int32, e.g. "-2147483648"
        number.TryFormat(buffer, out var charsWritten);
        for (var i = 0; i < charsWritten; i++)
            buffer[i] = buffer[i] == '-' ? '₋' : (char)(buffer[i] - '0' + '₀');
        return buffer[..charsWritten].ToString();
    }

    private static readonly IReadOnlyDictionary<WritingSystemId, int> EmptyWsOrder = new Dictionary<WritingSystemId, int>();

    // The alternative to display from a multi-writing-system value: the first non-empty one in the project's
    // configured writing-system order, falling back to WS code so the pick stays deterministic for a writing
    // system not in the order map (and in tests, where none is loaded). Null when every alternative is empty.
    private static string? BestAlternative<T>(
        IEnumerable<KeyValuePair<WritingSystemId, T>> alternatives,
        IReadOnlyDictionary<WritingSystemId, int> wsOrder,
        Func<T, string?> render)
    {
        foreach (var kvp in alternatives
            .OrderBy(kvp => wsOrder.GetValueOrDefault(kvp.Key, int.MaxValue))
            .ThenBy(kvp => kvp.Key.Code))
        {
            var text = render(kvp.Value);
            if (!string.IsNullOrEmpty(text)) return text;
        }
        return null;
    }

    // Rank each writing system by the project's configured display order (GetWritingSystems already sorts by
    // Order), so a label shows the alternative the editor lists first. A WsId shared by a vernacular and an
    // analysis system keeps its first rank; cross-type interleaving doesn't matter since a field holds one type.
    private static Dictionary<WritingSystemId, int> BuildWsOrder(WritingSystems writingSystems)
    {
        var rank = new Dictionary<WritingSystemId, int>();
        foreach (var ws in writingSystems.Vernacular.Concat(writingSystems.Analysis))
            rank.TryAdd(ws.WsId, rank.Count);
        return rank;
    }

    // Max grapheme clusters shown in an example-sentence snippet before it's truncated.
    // Small on purpose: it acts as a name/subject for the parent entity
    // Should arguably be handled in CSS, but it's nice having something subject-like to populate the Subject prop with.
    public const int TextSnippetBudget = 20;

    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);

    // A short one-line label for an example sentence: the best non-empty writing system's text (in configured
    // order, like Label), flattened from rich text and whitespace-collapsed, then truncated to a grapheme
    // budget. Null when no writing system has displayable text (mirrors the null gloss/headword degradation).
    internal static string? ExampleSnippet(RichMultiString sentence,
        IReadOnlyDictionary<WritingSystemId, int>? wsOrder = null)
    {
        return BestAlternative(sentence, wsOrder ?? EmptyWsOrder, richString =>
        {
            var plainText = Truncate(richString?.GetPlainText(), TextSnippetBudget);
            return string.IsNullOrWhiteSpace(plainText) ? null : plainText;
        });
    }

    // A short one-line label for a comment: its (plain-string) text, whitespace-collapsed and truncated to the
    // same budget as an example snippet. Null when the comment has no displayable text.
    private static string? CommentSnippet(string? text) => Truncate(text, TextSnippetBudget);

    // Truncate to at most `budget` grapheme clusters (never splitting a surrogate pair or combining mark),
    // appending an ellipsis when text is dropped. For space-separated scripts, back off to the last space in the
    // kept window so a word isn't cut mid-way; scripts without spaces (Thai, CJK, …) get a clean grapheme cut.
    // Works in logical order — visual placement of the ellipsis for RTL text is the renderer's job.
    private static string Truncate(string? text, int budget)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var collapsed = WhitespaceRun.Replace(text, " ").Trim();
        var clusterStarts = new List<int>();
        var enumerator = StringInfo.GetTextElementEnumerator(collapsed);
        while (enumerator.MoveNext()) clusterStarts.Add(enumerator.ElementIndex);
        if (clusterStarts.Count <= budget) return collapsed;

        var window = collapsed[..clusterStarts[budget]];
        var lastSpace = window.LastIndexOf(' ');
        // Only honour a space past the halfway point, so a long leading token can't collapse the snippet to almost nothing.
        if (lastSpace > window.Length / 2) window = window[..lastSpace];
        return window.TrimEnd() + "…";
    }
}
