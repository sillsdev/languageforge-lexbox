using MiniLcm.Models;

namespace MiniLcm.SyncHelpers;

/// <summary>
/// State for one sync walk over whole before/after entry sets: the global before/after context that
/// lets child-collection diffs recognize moves between parents, and the queue of genuine deletes
/// that runs after the walk. Child types that can't be moved yet (pictures, translations) get a
/// detect-and-throw context, so a move is a loud sync failure instead of silent delete+create.
/// </summary>
public class SyncContext
{
    /// <summary>
    /// For syncing objects without whole-project context (e.g. single-object update APIs): no move
    /// detection — every add is a create, every remove is a delete, and deletes run immediately.
    /// </summary>
    public static readonly SyncContext Empty = new();

    public DeferredDeletes DeferredDeletes { get; } = new();
    public MoveContext<Entry, Guid> Entries { get; }
    public MoveContext<Sense, Guid> Senses { get; }
    public MoveContext<ExampleSentence, Guid> Examples { get; }
    public MoveContext<Picture, Guid> Pictures { get; }
    public MoveContext<Translation, Guid> Translations { get; }

    private SyncContext()
    {
        Entries = MoveContext<Entry, Guid>.Empty;
        Senses = MoveContext<Sense, Guid>.Empty;
        Examples = MoveContext<ExampleSentence, Guid>.Empty;
        Pictures = MoveContext<Picture, Guid>.Empty;
        Translations = MoveContext<Translation, Guid>.Empty;
    }

    public SyncContext(Entry[] beforeEntries, Entry[] afterEntries)
    {
        Entries = MoveContext<Entry, Guid>.DeferredDeletesOnly(DeferredDeletes);
        Senses = MoveContext<Sense, Guid>.MovesSupported(AllSenses(beforeEntries), AllSenses(afterEntries), DeferredDeletes);
        Examples = MoveContext<ExampleSentence, Guid>.MovesSupported(AllExamples(beforeEntries), AllExamples(afterEntries), DeferredDeletes);
        Pictures = MoveContext<Picture, Guid>.MovesUnsupported(AllPictures(beforeEntries), AllPictures(afterEntries));
        Translations = MoveContext<Translation, Guid>.MovesUnsupported(AllTranslations(beforeEntries), AllTranslations(afterEntries));
    }

    // Create APIs assume every child in the payload is new; a payload containing a moved-in child
    // must be created empty instead and filled in by the child diff, which knows how to move.
    public bool HasChildMovingIn(Entry entry) => entry.Senses.Any(s => Senses.IsActuallyAMove(s.Id, out _) || HasChildMovingIn(s));
    public bool HasChildMovingIn(Sense sense) => sense.ExampleSentences.Any(e => Examples.IsActuallyAMove(e.Id, out _));

    /// <summary>
    /// The create-payload counterpart of the walk's move detection, for child types that can't move:
    /// creating a payload child that already exists elsewhere would duplicate it (or hit an opaque
    /// duplicate-guid error deep in a backend), so fail with the real reason instead.
    /// IsActuallyAMove never returns true here — for these types it throws when it detects a move.
    /// </summary>
    public void ThrowIfCreatingMovedChildren(Entry entry)
    {
        foreach (var sense in entry.Senses) ThrowIfCreatingMovedChildren(sense);
    }

    public void ThrowIfCreatingMovedChildren(Sense sense)
    {
        foreach (var picture in sense.Pictures) Pictures.IsActuallyAMove(picture.Id, out _);
        foreach (var example in sense.ExampleSentences) ThrowIfCreatingMovedChildren(example);
    }

    public void ThrowIfCreatingMovedChildren(ExampleSentence example)
    {
        foreach (var translation in example.Translations) Translations.IsActuallyAMove(translation.Id, out _);
    }

    private static Dictionary<Guid, Sense> AllSenses(Entry[] entries)
    {
        return entries.SelectMany(e => e.Senses).ToDictionary(s => s.Id);
    }

    private static Dictionary<Guid, ExampleSentence> AllExamples(Entry[] entries)
    {
        return entries.SelectMany(e => e.Senses).SelectMany(s => s.ExampleSentences).ToDictionary(e => e.Id);
    }

    private static Dictionary<Guid, Picture> AllPictures(Entry[] entries)
    {
        return entries.SelectMany(e => e.Senses).SelectMany(s => s.Pictures).ToDictionary(p => p.Id);
    }

    private static Dictionary<Guid, Translation> AllTranslations(Entry[] entries)
    {
#pragma warning disable CS0618 // the legacy placeholder id recurs across examples, so it can never identify a move
        return entries.SelectMany(e => e.Senses)
            .SelectMany(s => s.ExampleSentences)
            .SelectMany(x => x.Translations)
            .Where(t => t.Id != Translation.MissingTranslationId)
            .ToDictionary(t => t.Id);
#pragma warning restore CS0618
    }
}
