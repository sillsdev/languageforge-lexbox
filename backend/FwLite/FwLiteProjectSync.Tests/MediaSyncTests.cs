using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Media;
using FwLiteProjectSync.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Media;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// Repro for the media file-not-found sync crash (wayfinder map "Reproduce the media file-not-found
/// sync crash", ticket 06 — scenario 2: a file created in FwLite/CRDT whose binary was never uploaded,
/// so the referenced media id can't be resolved when writing the entry to FwData).
///
/// Today the CRDT->FwData sync throws MiniLcm.Exceptions.SyncObjectException (inner NotFoundException at
/// FwDataMiniLcmApi.FromMediaUri) and the whole sync job dies. These tests assert the DECIDED handling
/// (ticket 04): never crash — skip the unresolved audio field, leave the reference in the CRDT, and let a
/// later sync heal it once the file becomes resolvable. They are therefore RED until the fix lands, and are
/// committed to the effort branch to run in CI (not merged to develop until the fix).
///
/// Level note (research 02): these run through LocalMediaAdapter (the default adapter used by the tests),
/// which reproduces the exact throw site faithfully as long as the media URI uses the `localhost` authority.
/// The production FwHeadless reconcile that *deletes* the pending resource before the write is a separate
/// concern (thread B) and is not exercised here.
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
    /// The id LocalMediaAdapter would assign to <paramref name="fileName"/> under the project's
    /// LinkedFiles/AudioVisual folder — but the file is deliberately NOT created, so PathFromMediaUri
    /// returns null and FromMediaUri throws. Using the path-derived id lets the heal phase make the very
    /// same reference resolvable simply by creating the file at that path.
    /// </summary>
    private static (Guid fileId, string fullPath) UnresolvableAudioReference(SyncFixture fixture, string fileName)
    {
        var fullPath = Path.Combine(
            fixture.FwDataApi.Cache.LangProject.LinkedFilesRootDir,
            FwDataMiniLcmApi.AudioVisualFolder,
            fileName);
        return (LocalMediaAdapter.NewGuidV5(fullPath), fullPath);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Sync_CreateEntryWithUnresolvedAudio_SkipsFieldWithoutCrashing()
    {
        var fixture = SyncFixture.Create();
        await SetupAsync(fixture);
        try
        {
            var crdtApi = fixture.CrdtApi;
            var fwdataApi = fixture.FwDataApi;
            await fixture.SyncService.Import(crdtApi, fwdataApi);
            var snapshot = await fixture.RegenerateAndGetSnapshot();

            var (fileId, _) = UnresolvableAudioReference(fixture, "create-not-uploaded.wav");
            var mediaUri = new MediaUri(fileId, "localhost");
            var entryId = Guid.NewGuid();
            await crdtApi.CreateEntry(new Entry
            {
                Id = entryId,
                LexemeForm = { ["en"] = "rambuta" },
                CitationForm = { [AudioWs] = mediaUri.ToString() },
            });

            // DECIDED behavior: the unresolved audio ref is skipped and the sync completes (no crash).
            // Today this line throws SyncObjectException -> this test is the red repro.
            await fixture.SyncService.Sync(crdtApi, fwdataApi, snapshot);

            var fwdataEntry = await fwdataApi.GetEntry(entryId);
            fwdataEntry.Should().NotBeNull();
            fwdataEntry!.LexemeForm["en"].Should().Be("rambuta");
            fwdataEntry.CitationForm.Values.ContainsKey(AudioWs)
                .Should().BeFalse("the unresolved audio reference is skipped, not written to FwData");

            // The reference must survive in the CRDT so it can heal on a later sync (still pending).
            var crdtEntry = await crdtApi.GetEntry(entryId);
            crdtEntry!.CitationForm[AudioWs].Should().Be(mediaUri.ToString());
        }
        finally
        {
            fixture.DeleteSyncSnapshot();
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Sync_UpdateEntryAddingUnresolvedAudio_SkipsFieldWithoutCrashing()
    {
        var fixture = SyncFixture.Create();
        await SetupAsync(fixture);
        try
        {
            var crdtApi = fixture.CrdtApi;
            var fwdataApi = fixture.FwDataApi;
            await fixture.SyncService.Import(crdtApi, fwdataApi);
            var snapshot = await fixture.RegenerateAndGetSnapshot();

            // Create the entry with no audio and sync it across first (create path, no media -> no crash).
            var entryId = Guid.NewGuid();
            await crdtApi.CreateEntry(new Entry { Id = entryId, LexemeForm = { ["en"] = "rambuta" } });
            await fixture.SyncService.Sync(crdtApi, fwdataApi, snapshot);

            // Capture the synced baseline (entry present, no audio) BEFORE adding the audio. The snapshot
            // is the common ancestor for the next sync; regenerating it after the local change would make
            // the snapshot->fwdata diff look like "fwdata removed the audio" and revert it in the CRDT.
            var syncedSnapshot = await fixture.RegenerateAndGetSnapshot();

            // Now add the unresolved audio ref on the CRDT side and sync again (update path — the path the
            // reported stack trace took: UpdateEntry -> SetString -> FromMediaUri, wrapped as SyncObjectException).
            var before = await crdtApi.GetEntry(entryId);
            var after = before!.Copy();
            var (fileId, _) = UnresolvableAudioReference(fixture, "update-not-uploaded.wav");
            var mediaUri = new MediaUri(fileId, "localhost");
            after.CitationForm[AudioWs] = mediaUri.ToString();
            await crdtApi.UpdateEntry(before, after);

            // Today this throws SyncObjectException -> red repro.
            await fixture.SyncService.Sync(crdtApi, fwdataApi, syncedSnapshot);

            var fwdataEntry = await fwdataApi.GetEntry(entryId);
            fwdataEntry!.CitationForm.Values.ContainsKey(AudioWs)
                .Should().BeFalse("the unresolved audio reference is skipped on update, not written to FwData");
            var crdtEntry = await crdtApi.GetEntry(entryId);
            crdtEntry!.CitationForm[AudioWs].Should().Be(mediaUri.ToString());
        }
        finally
        {
            fixture.DeleteSyncSnapshot();
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Sync_UnresolvedAudio_HealsOnceFileIsResolvable()
    {
        var fixture = SyncFixture.Create();
        await SetupAsync(fixture);
        try
        {
            var crdtApi = fixture.CrdtApi;
            var fwdataApi = fixture.FwDataApi;
            await fixture.SyncService.Import(crdtApi, fwdataApi);
            var snapshot = await fixture.RegenerateAndGetSnapshot();

            var (fileId, fullPath) = UnresolvableAudioReference(fixture, "heal-not-uploaded.wav");
            var mediaUri = new MediaUri(fileId, "localhost");
            var entryId = Guid.NewGuid();
            await crdtApi.CreateEntry(new Entry
            {
                Id = entryId,
                LexemeForm = { ["en"] = "rambuta" },
                CitationForm = { [AudioWs] = mediaUri.ToString() },
            });

            // 1. First sync: the field is skipped, no crash.
            await fixture.SyncService.Sync(crdtApi, fwdataApi, snapshot);
            (await fwdataApi.GetEntry(entryId))!.CitationForm.Values.ContainsKey(AudioWs)
                .Should().BeFalse("first sync skips the still-unresolved audio reference");

            // The reference must remain PENDING in the CRDT so it can heal later. NOTE: the snapshot is
            // regenerated from the CRDT (issue #1912), so a naive skip leaves the snapshot holding the audio
            // while fwdata doesn't — and the *next* sync's snapshot-vs-fwdata diff would then delete the
            // reference from the CRDT (permanent data loss, no heal). The fix must prevent that revert; this
            // assertion + the heal below are the spec that guards it.
            (await crdtApi.GetEntry(entryId))!.CitationForm[AudioWs]
                .Should().Be(mediaUri.ToString(), "the skipped reference stays pending in the CRDT");

            // 2. The binary is uploaded — the file becomes resolvable under the same id.
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, "audio-bytes");
            var mediaAdapter = fixture.Services.GetRequiredService<IMediaAdapter>();
            var registered = mediaAdapter.MediaUriFromPath(fullPath, fwdataApi.Cache);
            registered.FileId.Should().Be(fileId, "the uploaded file must resolve to the id the CRDT referenced");

            // 3. The next sync heals automatically: the audio field now appears in FwData.
            var secondSnapshot = await fixture.RegenerateAndGetSnapshot();
            await fixture.SyncService.Sync(crdtApi, fwdataApi, secondSnapshot);

            var healed = await fwdataApi.GetEntry(entryId);
            healed!.CitationForm[AudioWs].Should().Be(mediaUri.ToString(),
                "once the file is resolvable the previously-skipped reference syncs to FwData");
        }
        finally
        {
            fixture.DeleteSyncSnapshot();
            await fixture.DisposeAsync();
        }
    }
}
