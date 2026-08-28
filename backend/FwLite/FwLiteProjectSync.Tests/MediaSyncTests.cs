using FluentAssertions.Execution;
using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Media;
using FwLiteProjectSync.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Media;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// Media sync tests that run through <see cref="LocalMediaAdapter"/> (the filesystem-backed adapter the
/// FwLite tests use). Under Option D the fix for a not-yet-uploaded binary (scenario 2) lives on the
/// PRODUCTION path — the FwHeadless <c>LexboxFwDataMediaAdapter</c> plus a pending <c>Files</c> DB row —
/// which LocalMediaAdapter has no equivalent for (there is no Files DB, so no pending-row concept). The
/// former scenario-2 proxy tests here depended on the removed <c>AudioSnapshotReconciler</c>'s
/// skip/heal/revert behavior and no longer describe intended behavior, so they were dropped (see
/// UnresolvedMediaSyncTests / SyncMediaFilesReconcileTests in backend/Testing for the production-path
/// coverage). What remains is the genuine FwData-side removal case, which the old reconciler wrongly
/// suppressed and which — with the reconciler gone — now propagates as an ordinary diff.
/// </summary>
public class MediaSyncTests
{
    private const string AudioWs = "en-Zxxx-x-audio";

    private static async Task SetupAsync(SyncFixture fixture)
    {
        await fixture.InitializeAsync();
        // Import copies this audio writing system into the CRDT, so both sides can carry an audio-WS value.
        await fixture.FwDataApi.CreateWritingSystem(new WritingSystem
        {
            Id = Guid.NewGuid(),
            WsId = AudioWs,
            Name = "English Audio",
            Abbreviation = "EN (A)",
            Font = "Arial",
            Type = WritingSystemType.Vernacular
        });
    }

    /// <summary>
    /// The id LocalMediaAdapter assigns to <paramref name="fileName"/> under the project's
    /// LinkedFiles/AudioVisual folder, paired with the full path. The id is derived from the path, so
    /// creating the file at that path makes the reference resolvable under the same id.
    /// </summary>
    private static (Guid fileId, string fullPath) AudioReference(SyncFixture fixture, string fileName)
    {
        var fullPath = Path.Combine(
            fixture.FwDataApi.Cache.LangProject.LinkedFilesRootDir,
            FwDataMiniLcmApi.AudioVisualFolder,
            fileName);
        return (LocalMediaAdapter.NewGuidV5(fullPath), fullPath);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Sync_RemoveResolvedAudioInFwData_RemovesItFromCrdt()
    {
        // A RESOLVABLE audio reference (file present) that syncs to both sides, then is deleted by a user in
        // FLEx. That FwData-side removal must propagate to the CRDT. The old AudioSnapshotReconciler could not
        // tell this genuine deletion apart from a skipped-pending reference and wrongly suppressed it (and the
        // reverse direction even resurrected it). With the reconciler removed under Option D this is an
        // ordinary snapshot-vs-fwdata diff and the removal propagates correctly.
        var fixture = SyncFixture.Create();
        await SetupAsync(fixture);
        try
        {
            var crdtApi = fixture.CrdtApi;
            var fwdataApi = fixture.FwDataApi;
            await fixture.SyncService.Import(crdtApi, fwdataApi);
            var snapshot = await fixture.RegenerateAndGetSnapshot();

            // Make the audio resolvable: write the file, then register it so it resolves under the same id.
            var (fileId, fullPath) = AudioReference(fixture, "remove-in-fw.wav");
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, "audio-bytes");
            var mediaAdapter = fixture.Services.GetRequiredService<IMediaAdapter>();
            mediaAdapter.MediaUriFromPath(fullPath, fwdataApi.Cache).FileId.Should().Be(fileId);
            var mediaUri = new MediaUri(fileId, "localhost");

            var entryId = Guid.NewGuid();
            await crdtApi.CreateEntry(new Entry
            {
                Id = entryId,
                LexemeForm = { ["en"] = "rambuta" },
                CitationForm = { [AudioWs] = mediaUri.ToString() },
            });

            // First sync: the resolvable audio syncs to FwData and stays in the CRDT — present on both sides.
            await fixture.SyncService.Sync(crdtApi, fwdataApi, snapshot);
            (await fwdataApi.GetEntry(entryId))!.CitationForm[AudioWs].Should().Be(mediaUri.ToString(),
                "guard: the resolvable audio must actually sync to FwData first");
            (await crdtApi.GetEntry(entryId))!.CitationForm[AudioWs].Should().Be(mediaUri.ToString(),
                "guard: the audio must be present in the CRDT before the FwData-side removal");

            // The synced baseline (audio present on both sides) is the ancestor for the next sync.
            var syncedSnapshot = await fixture.RegenerateAndGetSnapshot();

            // A FLEx user removes the audio reference on the FwData side.
            var fwBefore = await fwdataApi.GetEntry(entryId);
            var fwAfter = fwBefore!.Copy();
            fwAfter.CitationForm.Remove(AudioWs);
            await fwdataApi.UpdateEntry(fwBefore, fwAfter);
            (await fwdataApi.GetEntry(entryId))!.CitationForm.Values.ContainsKey(AudioWs)
                .Should().BeFalse("guard: the audio must actually be removed in FwData before syncing");

            // Sync: the FwData-side removal must propagate to the CRDT.
            await fixture.SyncService.Sync(crdtApi, fwdataApi, syncedSnapshot);

            using (new AssertionScope())
            {
                (await crdtApi.GetEntry(entryId))!.CitationForm.Values.ContainsKey(AudioWs)
                    .Should().BeFalse("a genuine FwData-side audio removal must be propagated to the CRDT, not suppressed");
                // Even worse than failing to propagate: if the CRDT still held the reference, the reverse
                // direction of the same sync would push it back to FwData, resurrecting the deleted reference.
                (await fwdataApi.GetEntry(entryId))!.CitationForm.Values.ContainsKey(AudioWs)
                    .Should().BeFalse("the FwData-side removal must not be resurrected by the sync pushing the CRDT value back");
            }
        }
        finally
        {
            fixture.DeleteSyncSnapshot();
            await fixture.DisposeAsync();
        }
    }
}
