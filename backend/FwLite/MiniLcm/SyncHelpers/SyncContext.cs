using MiniLcm.Exceptions;
using MiniLcm.Models;

namespace MiniLcm.SyncHelpers;

public class SyncContext
{
    public static readonly SyncContext Empty = new(null, null);

    /// <summary>Stays empty (DeleteAll is a no-op) for <see cref="Empty"/>.</summary>
    public DeferredDeletes DeferredDeletes { get; } = new();

    private readonly MoveDetection<Sense, Guid>? _senses;
    private readonly MoveDetection<ExampleSentence, Guid>? _examples;

    private SyncContext(MoveDetection<Sense, Guid>? senses, MoveDetection<ExampleSentence, Guid>? examples)
    {
        _senses = senses;
        _examples = examples;
    }

    public static SyncContext ForProjectSync(Entry[] beforeEntries, Entry[] afterEntries)
    {
        VerifyNoUnsupportedMoves(beforeEntries, afterEntries);
        return new(
            new MoveDetection<Sense, Guid>(AllSenses(beforeEntries), AllSenses(afterEntries)),
            new MoveDetection<ExampleSentence, Guid>(AllExamples(beforeEntries), AllExamples(afterEntries)));
    }

    /// <summary>Senses can't leave the entry, so only examples get move detection; the entry list is never diffed.</summary>
    public static SyncContext ForEntrySync(Entry beforeEntry, Entry afterEntry)
    {
        if (beforeEntry.Id != afterEntry.Id) throw new ArgumentException("Entry ids must match", nameof(afterEntry));
        VerifyNoUnsupportedMoves([beforeEntry], [afterEntry]);
        return new(null, // We're diffing a single entry, so senses don't need move detection.
         new MoveDetection<ExampleSentence, Guid>(AllExamples([beforeEntry]), AllExamples([afterEntry])));
    }

    // Decorator stack, outermost first. Each layer only sees what the layers above let through:
    //  1. MoveAware: an add/remove that is really a move becomes a reparent,
    //      so the layers below only ever see genuine creates and deletes.
    //  2. DeferringDeletes: queues those genuine deletes to run after the walk.
    //  3. MovedInChildren: a genuine create whose payload holds a moved-in child.
    //      Ensures that child is handled by move-aware sync code, rather than submitting
    //      the whole tree to the MiniLcmApi (which could throw on the live/duplicate guid).
    //  4. The real diff api.

    /// <summary>Entries don't move, but their deletes defer so a deleted entry's cascade runs after any sense is moved out of it.</summary>
    public CollectionDiffApi<Entry, Guid> EntriesDiffApi(IMiniLcmApi api)
    {
        var inner = new EntrySync.EntriesDiffApi(api, this);
        if (_senses is null) return inner;
        return new DeferringDeletesCollectionDiffApi<Entry, Guid>(
            new MovedInChildrenCollectionDiffApi<Entry, Guid>(inner,
                HasDescendantMovingIn,
                entry => entry with { Senses = [] },
                (created, entry) => created with { Senses = entry.Senses }),
            DeferredDeletes);
    }

    /// <summary>Deferred deletes and childless creates are for examples moving out of a deleted or into a created sense, so an entry sync needs them without sense move detection.</summary>
    public OrderableCollectionDiffApi<Sense, Guid> SensesDiffApi(IMiniLcmApi api, Guid entryId)
    {
        var inner = new EntrySync.SensesDiffApi(api, entryId, this);
        if (_examples is null) return inner;
        OrderableCollectionDiffApi<Sense, Guid> diffApi = new DeferringDeletesOrderableDiffApi<Sense, Guid>(
            new MovedInChildrenDiffApi<Sense, Guid>(inner, HasDescendantMovingIn, WithoutExamples),
            DeferredDeletes);
        if (_senses is null) return diffApi;
        return new MoveAwareOrderableDiffApi<Sense, Guid>(diffApi, _senses);
    }

    private static Sense WithoutExamples(Sense sense)
    {
        var copy = sense.Copy();
        copy.ExampleSentences = [];
        return copy;
    }

    /// <summary>Examples own no movable children, so no MovedInChildren layer.</summary>
    public OrderableCollectionDiffApi<ExampleSentence, Guid> ExampleSentencesDiffApi(IMiniLcmApi api, Guid entryId, Guid senseId)
    {
        var inner = new ExampleSentenceSync.ExampleSentencesDiffApi(api, entryId, senseId);
        if (_examples is null) return inner;
        return new MoveAwareOrderableDiffApi<ExampleSentence, Guid>(
            new DeferringDeletesOrderableDiffApi<ExampleSentence, Guid>(inner, DeferredDeletes),
            _examples);
    }

    private bool HasDescendantMovingIn(Entry entry) => entry.Senses.Any(s => ExistedBefore(_senses, s.Id) || HasDescendantMovingIn(s));
    private bool HasDescendantMovingIn(Sense sense) => sense.ExampleSentences.Any(e => ExistedBefore(_examples, e.Id));
    private static bool ExistedBefore<T>(MoveDetection<T, Guid>? moves, Guid id) => moves is not null && moves.ExistedBefore(id, out _);

    private static void VerifyNoUnsupportedMoves(Entry[] beforeEntries, Entry[] afterEntries)
    {
        ThrowIfContainsMoves(nameof(Picture), PictureParents(beforeEntries), PictureParents(afterEntries));
        ThrowIfContainsMoves(nameof(Translation), TranslationParents(beforeEntries), TranslationParents(afterEntries));
    }

    /// <summary>
    /// An id whose parent differs between the states is a move. The parent is the DIRECT parent, so a
    /// child riding along inside a moved sense or example is not itself a move.
    /// </summary>
    private static void ThrowIfContainsMoves(string typeName, Dictionary<Guid, Guid> beforeParents, Dictionary<Guid, Guid> afterParents)
    {
        foreach (var (id, beforeParent) in beforeParents)
        {
            if (afterParents.TryGetValue(id, out var afterParent) && afterParent != beforeParent)
                throw new MoveNotSupportedException(typeName, id, beforeParent, afterParent);
        }
    }

    private static Dictionary<Guid, Sense> AllSenses(Entry[] entries)
    {
        return entries.SelectMany(e => e.Senses).ToDictionary(s => s.Id);
    }

    private static Dictionary<Guid, ExampleSentence> AllExamples(Entry[] entries)
    {
        return entries.SelectMany(e => e.Senses).SelectMany(s => s.ExampleSentences).ToDictionary(e => e.Id);
    }

    // parent maps tolerate duplicate ids (First wins): a duplicated child is corrupt data the sync
    // otherwise handles, not something detection should turn into a hard failure
    private static Dictionary<Guid, Guid> PictureParents(Entry[] entries)
    {
        return entries.SelectMany(e => e.Senses)
            .SelectMany(s => s.Pictures.Select(p => (ChildId: p.Id, ParentId: s.Id)))
            .GroupBy(p => p.ChildId)
            .ToDictionary(g => g.Key, g => g.First().ParentId);
    }

    private static Dictionary<Guid, Guid> TranslationParents(Entry[] entries)
    {
#pragma warning disable CS0618 // the legacy placeholder id recurs across examples, so it can never identify a move
        return entries.SelectMany(e => e.Senses)
            .SelectMany(s => s.ExampleSentences)
            .SelectMany(x => x.Translations.Select(t => (ChildId: t.Id, ParentId: x.Id)))
            .Where(t => t.ChildId != Translation.MissingTranslationId)
            .GroupBy(t => t.ChildId)
            .ToDictionary(g => g.Key, g => g.First().ParentId);
#pragma warning restore CS0618
    }
}
