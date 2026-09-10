using MiniLcm.Exceptions;
using MiniLcm.Models;

namespace MiniLcm.SyncHelpers;

public class SyncContext
{
    /// <summary>No move detection: every add is a create and every delete runs immediately. For syncing a subtree on its own.</summary>
    public static readonly SyncContext Empty = new(deferDeletes: false,
        new Dictionary<Guid, Sense>(), new Dictionary<Guid, Sense>(),
        new Dictionary<Guid, ExampleSentence>(), new Dictionary<Guid, ExampleSentence>());

    private readonly bool _deferDeletes;
    private readonly DeferredDeletes _deferredDeletes = new();
    private readonly IReadOnlyDictionary<Guid, Sense> _sensesBefore;
    private readonly IReadOnlyDictionary<Guid, Sense> _sensesAfter;
    private readonly IReadOnlyDictionary<Guid, ExampleSentence> _examplesBefore;
    private readonly IReadOnlyDictionary<Guid, ExampleSentence> _examplesAfter;

    private SyncContext(bool deferDeletes,
        IReadOnlyDictionary<Guid, Sense> sensesBefore,
        IReadOnlyDictionary<Guid, Sense> sensesAfter,
        IReadOnlyDictionary<Guid, ExampleSentence> examplesBefore,
        IReadOnlyDictionary<Guid, ExampleSentence> examplesAfter)
    {
        _deferDeletes = deferDeletes;
        _sensesBefore = sensesBefore;
        _sensesAfter = sensesAfter;
        _examplesBefore = examplesBefore;
        _examplesAfter = examplesAfter;
    }

    public static SyncContext For(Entry[] beforeEntries, Entry[] afterEntries)
    {
        VerifyNoUnsupportedMoves(beforeEntries, afterEntries);
        return new SyncContext(deferDeletes: true,
            AllSenses(beforeEntries), AllSenses(afterEntries),
            AllExamples(beforeEntries), AllExamples(afterEntries));
    }

    public static SyncContext For(Entry beforeEntry, Entry afterEntry)
    {
        if (beforeEntry.Id != afterEntry.Id) throw new ArgumentException("Entry ids must match", nameof(afterEntry));
        return For([beforeEntry], [afterEntry]);
    }

    /// <summary>Used to differentiate an add/create from what is actually a reparent</summary>
    public Sense? ExistedBefore(Sense sense) => _sensesBefore.GetValueOrDefault(sense.Id);
    public ExampleSentence? ExistedBefore(ExampleSentence example) => _examplesBefore.GetValueOrDefault(example.Id);

    /// <summary>Used to differentiate a delete from what is actually a reparent</summary>
    public bool StillExists(Sense sense) => _sensesAfter.ContainsKey(sense.Id);
    public bool StillExists(ExampleSentence example) => _examplesAfter.ContainsKey(example.Id);

    // Empty tracks no moves, so nothing drains its queue; a caller reaching here would silently drop the delete.
    public Task<int> DeferDelete(Func<Task<int>> delete) => _deferDeletes
        ? _deferredDeletes.Defer(delete)
        : throw new InvalidOperationException("this SyncContext tracks no moves, so deferred deletes are never drained; delete inline instead");
    public Task<int> DeleteAll() => _deferredDeletes.DeleteAll();

    public bool HasMovedInDescendants(Entry entry) => entry.Senses.Any(s => ExistedBefore(s) is not null || HasMovedInDescendants(s));
    public bool HasMovedInDescendants(Sense sense) => sense.ExampleSentences.Any(e => ExistedBefore(e) is not null);

    /// <summary>A copy with moved-in descendants stripped. Genuinely new descendants are kept.</summary>
    public Entry WithoutMovedInDescendants(Entry entry) => entry with
    {
        Senses = [.. entry.Senses
            .Where(s => ExistedBefore(s) is null)
            .Select(s => HasMovedInDescendants(s) ? WithoutMovedInDescendants(s) : s)]
    };

    public Sense WithoutMovedInDescendants(Sense sense)
    {
        var copy = sense.Copy();
        copy.ExampleSentences = [.. sense.ExampleSentences.Where(e => ExistedBefore(e) is null)];
        return copy;
    }

    private static Dictionary<Guid, Sense> AllSenses(Entry[] entries)
    {
        return entries.SelectMany(e => e.Senses).ToDictionary(s => s.Id);
    }

    private static Dictionary<Guid, ExampleSentence> AllExamples(Entry[] entries)
    {
        return entries.SelectMany(e => e.Senses).SelectMany(s => s.ExampleSentences).ToDictionary(e => e.Id);
    }

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

/// <summary>Queues deletes so a subtree's children can be moved out before their old parent is deleted.</summary>
public class DeferredDeletes
{
    private readonly List<Func<Task<int>>> _deletes = [];

    /// <summary>Queues the delete; returns 0 changes now (they're counted when the queue is drained).</summary>
    public Task<int> Defer(Func<Task<int>> delete)
    {
        _deletes.Add(delete);
        return Task.FromResult(0);
    }

    public async Task<int> DeleteAll()
    {
        var changes = 0;
        // by index so a delete that queues another extends the drain instead of throwing
        for (var i = 0; i < _deletes.Count; i++)
            changes += await _deletes[i]();
        _deletes.Clear();
        return changes;
    }
}
