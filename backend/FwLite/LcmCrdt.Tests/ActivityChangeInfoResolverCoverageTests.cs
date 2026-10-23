using FluentAssertions.Execution;
using LcmCrdt.Tests.Changes;

namespace LcmCrdt.Tests;

public class ActivityChangeInfoResolverCoverageTests : HistoryServiceActivityTestsBase
{
    // Entity types for which a "subject" is not currently supported
    private static readonly Dictionary<string, string> IntentionallyDegraded = new()
    {
        ["RemoteResource"] = "Media upload plumbing (Harmony type, not IObjectWithId); the filename lives on " +
            "the resource service, not a projected table, and only one of its change types carries it inline.",
    };

    [Fact]
    public async Task EveryChangeEntityType_ResolvesASubject_OrIsIntentionallyDegraded()
    {
        await DataModel.AddChanges(ClientId, UseChangesTests.AllChangesInDependencyOrder());
        var activities = await Service.ProjectActivity(0, take: 1000, new ActivityQuery());

        // entity type name -> did EVERY change on that type resolve a non-null Subject (coverage), and did ANY
        // (degraded-allowlist rot-check). The catalogue populates a displayable label on every object, so a
        // handled type should resolve for all of its changes, not just one.
        var allResolved = new Dictionary<string, bool>();
        var anyResolved = new Dictionary<string, bool>();
        foreach (var activity in activities)
        {
            foreach (var change in activity.Changes)
            {
                var name = EntityTypeName(change.Entity.Change.EntityType);
                var hasSubject = change.Info.Subject is not null;
                allResolved[name] = allResolved.GetValueOrDefault(name, true) && hasSubject;
                anyResolved[name] = anyResolved.GetValueOrDefault(name) || hasSubject;
            }
        }

        using var _ = new AssertionScope();
        foreach (var (name, resolved) in allResolved)
        {
            if (IntentionallyDegraded.ContainsKey(name)) continue;
            resolved.Should().BeTrue($"a change on entity type '{name}' resolved no Subject in the activity feed; " +
                "give it a Subject arm in ActivityChangeInfoResolver (with an example test) " +
                $"or list it in {nameof(IntentionallyDegraded)} with a reason");
        }

        // Keep the allowlist from rotting: each listed type must actually be exercised, and must genuinely
        // degrade. If ANY change on it starts resolving a Subject, it's no longer fully degraded — drop it from
        // the list (a type typically gains resolution one change at a time, so check "any", not "all").
        foreach (var name in IntentionallyDegraded.Keys)
        {
            anyResolved.Should().ContainKey(name,
                $"'{name}' is listed as intentionally degraded but no such change was exercised");
            anyResolved.GetValueOrDefault(name).Should().BeFalse(
                $"'{name}' is listed as intentionally degraded but now resolves a Subject; remove it from {nameof(IntentionallyDegraded)}");
        }
    }

    // Entity types whose list row names an owning entry that the detail view doesn't link to
    private static readonly Dictionary<string, string> DetailOmitsOwningEntry = new()
    {
        ["CommentThread"] = "The detail preview has no comment layout; linking the entry would show it as deleted.",
        ["UserComment"] = "The detail preview has no comment layout; linking the entry would show it as deleted.",
    };

    [Fact]
    public async Task EveryChange_LoadsADetailContext_LinkedToTheListRowsOwningEntry()
    {
        await DataModel.AddChanges(ClientId, UseChangesTests.AllChangesInDependencyOrder());
        var activities = await Service.ProjectActivity(0, take: 1000, new ActivityQuery());

        var omitted = new HashSet<string>();
        using var _ = new AssertionScope();
        foreach (var activity in activities)
        {
            foreach (var change in activity.Changes)
            {
                var name = EntityTypeName(change.Entity.Change.EntityType);
                var changeName = change.Entity.Change.GetType().Name;
                var context = (await Service.Awaiting(s => s.LoadChangeContext(activity.CommitId, change.Entity.Index))
                    .Should().NotThrowAsync($"the detail view must open for {changeName}")).Which;
                if (context is null || change.Info.OwningEntryId is not { } owningEntryId) continue;

                if (DetailOmitsOwningEntry.ContainsKey(name))
                {
                    context.AffectedEntries.Select(e => e.Id).Should().NotContain(owningEntryId,
                        $"'{name}' is listed in {nameof(DetailOmitsOwningEntry)} but {changeName} now links its owning entry; remove it from the list");
                    omitted.Add(name);
                    continue;
                }
                context.AffectedEntries.Select(e => e.Id).Should().Contain(owningEntryId,
                    $"the list row for {changeName} names an owning entry, so the detail view should link to it; " +
                    $"handle '{name}' in HistoryService.GetAffectedEntryIds or list it in {nameof(DetailOmitsOwningEntry)} with a reason");
            }
        }

        omitted.Should().BeEquivalentTo(DetailOmitsOwningEntry.Keys,
            $"each type in {nameof(DetailOmitsOwningEntry)} must be exercised");
    }

    // Entity type name without the CLR generic-arity suffix, so a generic entity (RemoteResource<TMetadata>)
    // reads as "RemoteResource". The resolved entity types are all non-generic, so stripping is harmless there.
    // EntityType is null for opaque/unknown changes; none are expected in these fixtures, so bucket them
    // under a distinct name rather than throwing.
    private static string EntityTypeName(Type? entityType) => entityType?.Name.Split('`')[0] ?? "(unknown)";
}
