using MiniLcm.Exceptions;
using MiniLcm.Models;

namespace MiniLcm.SyncHelpers;

/// <summary>
/// State for one sync walk over whole before/after entry sets: the global before/after context that
/// lets child-collection diffs recognize moves between parents, and the queue of genuine deletes
/// that runs after the walk. Child types that can't be moved yet (pictures, translations) are
/// rejected up front by the constructor — before anything is written — so a move of those is a
/// loud, all-or-nothing sync failure instead of a silent delete+create.
/// </summary>
public class SyncContext
{
    /// <summary>
    /// For syncing objects without whole-project context (e.g. single-object update APIs): no move
    /// detection — every add is a create, every remove is a delete, and deletes run immediately.
    /// </summary>
    public static readonly SyncContext Empty = new();

    private readonly DeferredDeletes? _deferredDeletes;
    public DeferredDeletes DeferredDeletes => _deferredDeletes ?? throw new InvalidOperationException("SyncContext.Empty has no delete queue");
    public MoveContext<Entry, Guid> Entries { get; }
    public MoveContext<Sense, Guid> Senses { get; }
    public MoveContext<ExampleSentence, Guid> Examples { get; }
    // moves of these types are rejected up front (see ThrowIfMoved), so their diffs treat every add/remove as genuine
    public MoveContext<Picture, Guid> Pictures => MoveContext<Picture, Guid>.Empty;
    public MoveContext<Translation, Guid> Translations => MoveContext<Translation, Guid>.Empty;

    private SyncContext()
    {
        Entries = MoveContext<Entry, Guid>.Empty;
        Senses = MoveContext<Sense, Guid>.Empty;
        Examples = MoveContext<ExampleSentence, Guid>.Empty;
    }

    public SyncContext(Entry[] beforeEntries, Entry[] afterEntries)
    {
        _deferredDeletes = new DeferredDeletes();
        Entries = MoveContext<Entry, Guid>.DeferredDeletesOnly(_deferredDeletes);
        Senses = MoveContext<Sense, Guid>.MovesSupported(AllSenses(beforeEntries), AllSenses(afterEntries), _deferredDeletes);
        Examples = MoveContext<ExampleSentence, Guid>.MovesSupported(AllExamples(beforeEntries), AllExamples(afterEntries), _deferredDeletes);
        ThrowIfMoved(nameof(Picture), PictureParents(beforeEntries), PictureParents(afterEntries));
        ThrowIfMoved(nameof(Translation), TranslationParents(beforeEntries), TranslationParents(afterEntries));
    }

    // Create APIs assume every child in the payload is new; a payload containing a moved-in child
    // must be created empty instead and filled in by the child diff, which knows how to move.
    public bool HasChildMovingIn(Entry entry) => entry.Senses.Any(s => Senses.IsActuallyAMove(s.Id, out _) || HasChildMovingIn(s));
    public bool HasChildMovingIn(Sense sense) => sense.ExampleSentences.Any(e => Examples.IsActuallyAMove(e.Id, out _));

    /// <summary>
    /// An id whose parent differs between the states is a move. The parent is the DIRECT parent, so a
    /// child riding along inside a moved sense or example is not itself a move.
    /// </summary>
    private static void ThrowIfMoved(string typeName, Dictionary<Guid, Guid> beforeParents, Dictionary<Guid, Guid> afterParents)
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
