using MiniLcm.Media;
using MiniLcm.Models;

namespace MiniLcm.SyncHelpers;

/// <summary>
/// Reconciles audio writing-system values in a project snapshot against what FwData actually holds, so the
/// FwData→CRDT diff does not revert an audio reference that was intentionally skipped on write.
///
/// The snapshot is regenerated from the CRDT (issue #1912), so it carries an unresolved audio reference that
/// the CRDT→FwData write skipped (tickets 04/13). FwData therefore lacks that value. Left alone, the next
/// sync's snapshot-vs-fwdata diff would emit a <c>remove</c> and delete the still-pending reference from the
/// CRDT — permanent data loss, no heal. Dropping the audio keys the snapshot has but FwData lacks makes that
/// diff a no-op, while the CRDT→FwData direction (which reads the live CRDT, not the snapshot) still
/// re-attempts the write and heals once the binary becomes resolvable.
///
/// Only audio keys FwData can't resolve (absent, or held as the not-found sentinel — an unresolvable value we
/// must not let overwrite a real CRDT reference) are neutralized; they are removed from BOTH the snapshot and
/// the FwData copy so the diff produces no operation for them. Audio FwData genuinely holds is left intact, so
/// a legitimate FieldWorks-side audio addition or change still syncs FwData→CRDT.
///
/// Mutates both entry arrays in place. The snapshot is caller-owned and post-sync it is regenerated from the
/// CRDT (#1912), so the mutation doesn't leak into persisted state; the FwData array is the live read used by
/// both sync directions this pass.
/// </summary>
public static class AudioSnapshotReconciler
{
    public static void NeutralizeUnresolvableAudio(Entry[] snapshotEntries, Entry[] fwDataEntries)
    {
        var fwById = fwDataEntries.ToDictionary(e => e.Id);
        foreach (var snapshotEntry in snapshotEntries)
        {
            if (!fwById.TryGetValue(snapshotEntry.Id, out var fwEntry)) continue;
            ReconcileEntry(snapshotEntry, fwEntry);
        }
    }

    private static void ReconcileEntry(Entry snapshot, Entry fwData)
    {
        DropAudio(snapshot.LexemeForm, fwData.LexemeForm);
        DropAudio(snapshot.CitationForm, fwData.CitationForm);
        DropAudio(snapshot.LiteralMeaning, fwData.LiteralMeaning);
        DropAudio(snapshot.Note, fwData.Note);

        var fwSenses = fwData.Senses.ToDictionary(s => s.Id);
        foreach (var snapshotSense in snapshot.Senses)
        {
            if (!fwSenses.TryGetValue(snapshotSense.Id, out var fwSense)) continue;
            DropAudio(snapshotSense.Gloss, fwSense.Gloss);
            DropAudio(snapshotSense.Definition, fwSense.Definition);

            var fwExamples = fwSense.ExampleSentences.ToDictionary(e => e.Id);
            foreach (var snapshotExample in snapshotSense.ExampleSentences)
            {
                if (!fwExamples.TryGetValue(snapshotExample.Id, out var fwExample)) continue;
                DropAudio(snapshotExample.Sentence, fwExample.Sentence);

                var fwTranslations = fwExample.Translations.ToDictionary(t => t.Id);
                foreach (var snapshotTranslation in snapshotExample.Translations)
                {
                    if (!fwTranslations.TryGetValue(snapshotTranslation.Id, out var fwTranslation)) continue;
                    DropAudio(snapshotTranslation.Text, fwTranslation.Text);
                }
            }
        }
    }

    // For every audio key that FwData can't resolve (absent, empty, or the not-found sentinel), remove it from
    // BOTH sides so the FwData→CRDT diff produces no operation for it — neither a `remove` (which would delete a
    // pending reference) nor an `add`/`replace` of the sentinel (which would overwrite a real CRDT reference with
    // an identity-free value that can never heal). Removing it from the FwData copy is safe for the reverse
    // (CRDT→FwData) diff: that direction then re-adds the live CRDT value and writes it when it becomes resolvable.
    private static void DropAudio(MultiString snapshot, MultiString fwData)
    {
        foreach (var key in AudioKeys(snapshot.Values.Keys, fwData.Values.Keys))
        {
            if (!fwData.Values.TryGetValue(key, out var value) || IsUnresolvable(value))
            {
                snapshot.Remove(key);
                fwData.Remove(key);
            }
        }
    }

    private static void DropAudio(RichMultiString snapshot, RichMultiString fwData)
    {
        foreach (var key in AudioKeys(snapshot.Keys, fwData.Keys))
        {
            if (!fwData.TryGetValue(key, out var value) || IsUnresolvable(value?.GetPlainText()))
            {
                snapshot.Remove(key);
                fwData.Remove(key);
            }
        }
    }

    private static WritingSystemId[] AudioKeys(
        IEnumerable<WritingSystemId> snapshotKeys,
        IEnumerable<WritingSystemId> fwDataKeys)
        => snapshotKeys.Concat(fwDataKeys).Where(k => k.IsAudio).Distinct().ToArray();

    private static bool IsUnresolvable(string? value)
        => string.IsNullOrEmpty(value) || value == MediaUri.NotFoundString;
}
