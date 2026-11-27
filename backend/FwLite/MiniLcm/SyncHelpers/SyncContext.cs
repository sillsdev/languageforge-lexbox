using System.Collections.Concurrent;
using MiniLcm.Exceptions;
using MiniLcm.Models;

namespace MiniLcm.SyncHelpers;

public class SyncContext
{
    private readonly bool _deferDeletes;
    private readonly DeferredDeletes _deferredDeletes = new();
    private readonly Descendants _before;
    private readonly Descendants _after;

    private SyncContext(bool deferDeletes, Descendants before, Descendants after)
    {
        _deferDeletes = deferDeletes;
        _before = before;
        _after = after;
    }

    private record Descendants(
        IReadOnlyDictionary<Guid, Sense> Senses,
        IReadOnlyDictionary<Guid, ExampleSentence> Examples,
        IReadOnlyDictionary<Guid, Picture> Pictures);

    /// <summary>Always defers deletes so descendants can move between entries; the caller must call <see cref="FlushDeletes"/>.</summary>
    public static SyncContext For(Entry[] beforeEntries, Entry[] afterEntries)
    {
        VerifyNoUnsupportedMoves(beforeEntries, afterEntries);
        return new SyncContext(deferDeletes: true, All(beforeEntries), All(afterEntries));
    }

    public static SyncContext For(Entry beforeEntry, Entry afterEntry)
    {
        if (beforeEntry.Id != afterEntry.Id) throw new ArgumentException("Entry ids must match", nameof(afterEntry));
        return For([beforeEntry], [afterEntry]);
    }

    public static SyncContext For(Sense beforeSense, Sense afterSense, bool deferDeletes = false)
    {
        if (beforeSense.Id != afterSense.Id) throw new ArgumentException("Sense ids must match", nameof(afterSense));
        return new SyncContext(deferDeletes, Of(beforeSense), Of(afterSense));
    }

    /// <summary>Used to differentiate an add/create from what is actually a reparent</summary>
    public Sense? ExistedBefore(Sense sense) => _before.Senses.GetValueOrDefault(sense.Id);
    public ExampleSentence? ExistedBefore(ExampleSentence example) => _before.Examples.GetValueOrDefault(example.Id);
    public Picture? ExistedBefore(Picture picture) => _before.Pictures.GetValueOrDefault(picture.Id);

    /// <summary>Used to differentiate a delete from what is actually a reparent</summary>
    public bool StillExists(Sense sense) => _after.Senses.ContainsKey(sense.Id);
    public bool StillExists(ExampleSentence example) => _after.Examples.ContainsKey(example.Id);
    public bool StillExists(Picture picture) => _after.Pictures.ContainsKey(picture.Id);

    public Task<int> HandleDelete(Func<Task<int>> delete) => _deferDeletes
        ? _deferredDeletes.Defer(delete)
        : delete.Invoke();
    public Task<int> FlushDeletes() => _deferredDeletes.DeleteAll();

    public bool HasMovedInDescendants(Entry entry) => entry.Senses.Any(s => ExistedBefore(s) is not null || HasMovedInDescendants(s));
    public bool HasMovedInDescendants(Sense sense) =>
        sense.ExampleSentences.Any(e => ExistedBefore(e) is not null) || sense.Pictures.Any(p => ExistedBefore(p) is not null);

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
        copy.Pictures = [.. sense.Pictures.Where(p => ExistedBefore(p) is null)];
        return copy;
    }

    private static Descendants All(Entry[] entries) => Of(entries.SelectMany(e => e.Senses).ToArray());

    private static Descendants Of(params Sense[] senseList)
    {
        //preallocating from the counts avoids resizing the dictionaries as they grow; counting first is cheaper than resizing
        Dictionary<Guid, Sense> senses = new(senseList.Length);
        Dictionary<Guid, ExampleSentence> examples = new(senseList.Sum(s => s.ExampleSentences.Count));
        Dictionary<Guid, Picture> pictures = new(senseList.Sum(s => s.Pictures.Count));
        foreach (var sense in senseList)
        {
            senses[sense.Id] = sense;
            foreach (var example in sense.ExampleSentences)
                examples[example.Id] = example;
            foreach (var picture in sense.Pictures)
                pictures[picture.Id] = picture;
        }
        return new Descendants(senses, examples, pictures);
    }

    private static void VerifyNoUnsupportedMoves(Entry[] beforeEntries, Entry[] afterEntries)
    {
        ThrowIfContainsMoves(nameof(Translation), TranslationParents(beforeEntries), TranslationParents(afterEntries));
    }

    /// <summary>
    /// An id whose parent differs between the states is a move. The parent is the DIRECT parent, so a
    /// child riding along inside a moved sense or example is not itself a move.
    /// </summary>
    private static void ThrowIfContainsMoves(string typeName, IEnumerable<(Guid ChildId, Guid ParentId)> beforeParents, IEnumerable<(Guid ChildId, Guid ParentId)> afterParents)
    {
        var afterParentsLookup = afterParents.ToDictionary(p => p.ChildId, p => p.ParentId);
        foreach (var (id, beforeParent) in beforeParents)
        {
            if (afterParentsLookup.TryGetValue(id, out var afterParent) && afterParent != beforeParent)
                throw new MoveNotSupportedException(typeName, id, beforeParent, afterParent);
        }
    }

    // the parent map tolerates duplicate ids (First wins): a duplicated child is corrupt data the sync
    // otherwise handles, not something detection should turn into a hard failure
    private static IEnumerable<(Guid ChildId, Guid ParentId)> TranslationParents(Entry[] entries)
    {
        return entries.SelectMany(e => e.Senses)
            .SelectMany(s => s.ExampleSentences)
            .SelectMany(x => x.Translations, (x, t) => (ChildId: t.Id, ParentId: x.Id))
            .Where(t => !Translation.IsMissingTranslationId(t.ChildId))// the legacy placeholder id recurs across examples, so it can never identify a move
            .GroupBy(t => t.ChildId)
            .Select(g => (ChildId: g.Key, ParentId: g.First().ParentId));
    }
}

/// <summary>Queues deletes so a subtree's children can be moved out before their old parent is deleted.</summary>
public class DeferredDeletes
{
    private readonly ConcurrentQueue<Func<Task<int>>> _deletes = new();

    /// <summary>Queues the delete; returns 0 changes now (they're counted when the queue is drained).</summary>
    public Task<int> Defer(Func<Task<int>> delete)
    {
        _deletes.Enqueue(delete);
        return Task.FromResult(0);
    }

    public async Task<int> DeleteAll()
    {
        var changes = 0;
        // dequeue as we go so a delete that queues another extends the drain
        while (_deletes.TryDequeue(out var delete))
            changes += await delete();
        return changes;
    }
}
