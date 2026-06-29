using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
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
/// Reads the projected snapshot tables, so labels reflect the current state; deleted entities get the "(Unknown)" headword.
/// </summary>
internal static class ActivityChangeInfoResolver
{
    public static async Task ResolveAsync(ICrdtDbContext db, IReadOnlyList<ProjectActivity> activities)
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
                case AddEntryComponentChange a: entryIds.Add(a.ComponentEntryId); break;
                case SetComplexFormComponentChange { ComponentEntryId: { } cid }: entryIds.Add(cid); break;
            }
        }

        // Walk down to the root entry: component → entry, example → sense → entry. Load both ends of a component link so we can name either.
        var components = await LoadByIds<ComplexFormComponent>(db, componentIds);
        foreach (var component in components.Values)
        {
            entryIds.Add(component.ComplexFormEntryId);
            entryIds.Add(component.ComponentEntryId);
        }
        var examples = await LoadByIds<ExampleSentence>(db, exampleIds);
        foreach (var example in examples.Values) senseIds.Add(example.SenseId);
        var senses = await LoadByIds<Sense>(db, senseIds);
        foreach (var sense in senses.Values) entryIds.Add(sense.EntryId);

        var entries = await LoadByIds<Entry>(db, entryIds);
        var partsOfSpeech = await LoadByIds<PartOfSpeech>(db, partOfSpeechIds);
        var semanticDomains = await LoadByIds<SemanticDomain>(db, semanticDomainIds);
        var publications = await LoadByIds<Publication>(db, publicationIds);
        var complexFormTypes = await LoadByIds<ComplexFormType>(db, complexFormTypeIds);
        var morphTypes = await LoadByIds<MorphType>(db, morphTypeIds);

        string Headword(Guid entryId) => entries.TryGetValue(entryId, out var entry) ? HeadwordWithHomograph(entry) : Entry.UnknownHeadword;

        foreach (var activity in activities)
        {
            activity.ChangeInfo = activity.Changes
                .Select(change => Build(change, Headword))
                .ToList();
        }

        ActivityChangeInfo Build(ChangeEntity<IChange> change, Func<Guid, string> headword)
        {
            if (ResolveReorder(change) is { } reorder) return reorder;
            var (subject, rootEntryId) = ResolveSubject(change, headword);
            return new ActivityChangeInfo(subject, rootEntryId, TargetLabel(change));
        }

        // A reorder reads best as "<container> · Reordered <item> <name>": the subject is the parent whose list changed, the target is the moved item.
        ActivityChangeInfo? ResolveReorder(ChangeEntity<IChange> change)
        {
            switch (change.Change)
            {
                case Changes.SetOrderChange<Sense> when senses.TryGetValue(change.EntityId, out var sense):
                    return new ActivityChangeInfo(Headword(sense.EntryId), sense.EntryId, Label(sense.Gloss));
                case Changes.SetOrderChange<ExampleSentence> when examples.TryGetValue(change.EntityId, out var ex) && senses.TryGetValue(ex.SenseId, out var exSense):
                    return new ActivityChangeInfo(SenseLabel(Headword(exSense.EntryId), exSense.Gloss), exSense.EntryId, null);
                case Changes.SetOrderChange<ComplexFormComponent> when components.TryGetValue(change.EntityId, out var comp):
                    return new ActivityChangeInfo(Headword(comp.ComplexFormEntryId), comp.ComplexFormEntryId,
                        entries.ContainsKey(comp.ComponentEntryId) ? Headword(comp.ComponentEntryId) : null);
                default:
                    return null;
            }
        }

        (string? Subject, Guid? RootEntryId) ResolveSubject(ChangeEntity<IChange> change, Func<Guid, string> headword)
        {
            var id = change.EntityId;
            switch (BucketFor(change.Change.EntityType))
            {
                case nameof(Entry):
                    return (headword(id), id);
                case nameof(Sense) when senses.TryGetValue(id, out var sense):
                    return (SenseLabel(headword(sense.EntryId), sense.Gloss), sense.EntryId);
                case nameof(ExampleSentence) when examples.TryGetValue(id, out var ex) && senses.TryGetValue(ex.SenseId, out var exSense):
                    return (SenseLabel(headword(exSense.EntryId), exSense.Gloss), exSense.EntryId);
                case nameof(ComplexFormComponent) when components.TryGetValue(id, out var component):
                    return (headword(component.ComplexFormEntryId), component.ComplexFormEntryId);
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
            AddEntryComponentChange a when entries.TryGetValue(a.ComponentEntryId, out var component) => HeadwordWithHomograph(component),
            SetComplexFormComponentChange { ComponentEntryId: { } cid } when entries.TryGetValue(cid, out var component) => HeadwordWithHomograph(component),
            _ => null
        };
    }

    private static string BucketFor(Type entityType) => entityType.Name;

    private static async Task<Dictionary<Guid, T>> LoadByIds<T>(ICrdtDbContext db, HashSet<Guid> ids)
        where T : class, IObjectWithId
    {
        if (ids.Count == 0) return [];
        var loaded = await db.Set<T>().Where(o => ids.Contains(o.Id)).ToListAsyncLinqToDB();
        return loaded.ToDictionary(o => o.Id);
    }

    private static string? SenseLabel(string headword, MultiString gloss)
    {
        var glossText = Label(gloss);
        return glossText is null ? headword : $"{headword} › {glossText}";
    }

    private static string? Label(MultiString multiString) =>
        multiString.Values.Select(kvp => kvp.Value?.Trim()).FirstOrDefault(text => !string.IsNullOrEmpty(text));

    // FieldWorks distinguishes same-spelled entries by a homograph number shown as a subscript; it's only assigned (> 0) when there's a collision.
    private static string HeadwordWithHomograph(Entry entry)
    {
        var headword = entry.Headword();
        return entry.HomographNumber > 0 ? headword + Subscript(entry.HomographNumber) : headword;
    }

    private static string Subscript(int number) =>
        new(number.ToString().Select(digit => (char)('₀' + (digit - '0'))).ToArray());

    // The app shows a domain as "code name" (e.g. "5.2 Food"); the code alone is too cryptic to identify it.
    private static string SemanticDomainLabel(SemanticDomain domain) =>
        Label(domain.Name) is { } name ? $"{domain.Code} {name}" : domain.Code;
}
