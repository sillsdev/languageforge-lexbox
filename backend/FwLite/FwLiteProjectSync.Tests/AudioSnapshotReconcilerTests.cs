using MiniLcm.Media;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// Unit tests for <see cref="AudioSnapshotReconciler"/>, the guard that keeps the FwData→CRDT sync diff from
/// reverting or clobbering a media reference the CRDT→FwData write skipped as unresolvable. The snapshot is
/// regenerated from the CRDT (#1912); these tests pin that an audio value FwData can't resolve (absent or the
/// not-found sentinel) is neutralized on BOTH sides so the diff is a no-op, while resolvable audio is left
/// intact so a real FieldWorks-side change still syncs.
/// </summary>
public class AudioSnapshotReconcilerTests
{
    private const string AudioWs = "en-Zxxx-x-audio";
    private static string RealRef() => new MediaUri(Guid.NewGuid(), "localhost").ToString();

    private static Entry EntryWithCitation(Guid id, MultiString citationForm) =>
        new() { Id = id, LexemeForm = { ["en"] = "rambuta" }, CitationForm = citationForm };

    [Fact]
    public void NeutralizesPendingReferenceAbsentInFwData()
    {
        // The skip/heal case: the CRDT→FwData write skipped the ref, so FwData lacks the audio key while the
        // snapshot (from the CRDT) still holds it. Without neutralizing, the diff emits a `remove` that would
        // delete the still-pending reference from the CRDT.
        var id = Guid.NewGuid();
        var reference = RealRef();
        var snapshot = new[] { EntryWithCitation(id, new MultiString { { AudioWs, reference } }) };
        var fwData = new[] { EntryWithCitation(id, new MultiString()) };

        AudioSnapshotReconciler.NeutralizeUnresolvableAudio(snapshot, fwData);

        snapshot[0].CitationForm.Values.Should().NotContainKey(AudioWs, "the pending reference must not diff to a remove");
        fwData[0].CitationForm.Values.Should().NotContainKey(AudioWs);
    }

    [Fact]
    public void NeutralizesWhenFwDataHoldsNotFoundSentinel()
    {
        // Regression for the reconciler's own data-loss risk: FwData surfaces the not-found sentinel (e.g. an
        // out-of-tree rooted path) while the CRDT holds a real reference. Only dropping the snapshot key would
        // turn the diff into an `add`/`replace` of the sentinel and clobber the real CRDT reference with an
        // identity-free value that can never heal. Both sides must be cleared so the diff is a no-op.
        var id = Guid.NewGuid();
        var snapshot = new[] { EntryWithCitation(id, new MultiString { { AudioWs, RealRef() } }) };
        var fwData = new[] { EntryWithCitation(id, new MultiString { { AudioWs, MediaUri.NotFoundString } }) };

        AudioSnapshotReconciler.NeutralizeUnresolvableAudio(snapshot, fwData);

        snapshot[0].CitationForm.Values.Should().NotContainKey(AudioWs);
        fwData[0].CitationForm.Values.Should().NotContainKey(AudioWs,
            "the sentinel must not diff into the CRDT and overwrite the real reference");
    }

    [Fact]
    public void KeepsResolvableFwDataAudioSoFieldWorksAdditionsStillSync()
    {
        // A genuine FieldWorks-side audio addition: FwData holds a resolvable reference the snapshot lacks.
        // It must survive so the FwData→CRDT diff still adds it to the CRDT.
        var id = Guid.NewGuid();
        var reference = RealRef();
        var snapshot = new[] { EntryWithCitation(id, new MultiString()) };
        var fwData = new[] { EntryWithCitation(id, new MultiString { { AudioWs, reference } }) };

        AudioSnapshotReconciler.NeutralizeUnresolvableAudio(snapshot, fwData);

        fwData[0].CitationForm[AudioWs].Should().Be(reference, "a resolvable FwData audio reference must be preserved");
    }

    [Fact]
    public void LeavesNonAudioWritingSystemsUntouched()
    {
        var id = Guid.NewGuid();
        var snapshot = new[] { EntryWithCitation(id, new MultiString { { "en", "before" } }) };
        var fwData = new[] { EntryWithCitation(id, new MultiString()) };

        AudioSnapshotReconciler.NeutralizeUnresolvableAudio(snapshot, fwData);

        snapshot[0].CitationForm["en"].Should().Be("before", "non-audio writing systems must diff normally");
    }
}
