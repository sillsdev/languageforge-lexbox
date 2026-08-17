using MiniLcm.Models;

namespace MiniLcm.SyncHelpers;

/// <summary>
/// State for one sync walk over whole before/after entry sets: the global before/after context that
/// lets child-collection diffs recognize moves, and the queue of genuine deletes that runs after the walk.
/// </summary>
public class EntrySyncContext
{
    public DeferredDeletes DeferredDeletes { get; } = new();
    public MoveContext<Sense, Guid> Senses { get; }
    public MoveContext<ExampleSentence, Guid> Examples { get; }

    public EntrySyncContext(Entry[] beforeEntries, Entry[] afterEntries)
    {
        Senses = new(AllSenses(beforeEntries), AllSenses(afterEntries), DeferredDeletes);
        Examples = new(AllExamples(beforeEntries), AllExamples(afterEntries), DeferredDeletes);
    }

    // Create APIs assume every child in the payload is new; a payload containing a moved-in child
    // must be created empty instead and filled in by the child diff, which knows how to move.
    public bool HasChildMovingIn(Entry entry) => entry.Senses.Any(s => Senses.IsActuallyAMove(s.Id, out _) || HasChildMovingIn(s));
    public bool HasChildMovingIn(Sense sense) => sense.ExampleSentences.Any(e => Examples.IsActuallyAMove(e.Id, out _));

    private static Dictionary<Guid, Sense> AllSenses(Entry[] entries)
    {
        return entries.SelectMany(e => e.Senses).ToDictionary(s => s.Id);
    }

    private static Dictionary<Guid, ExampleSentence> AllExamples(Entry[] entries)
    {
        return entries.SelectMany(e => e.Senses).SelectMany(s => s.ExampleSentences).ToDictionary(e => e.Id);
    }
}
