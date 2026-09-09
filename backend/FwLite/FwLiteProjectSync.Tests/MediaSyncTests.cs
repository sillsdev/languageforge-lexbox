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
/// FwLite tests use). For FwHeadless tests see SyncMediaFilesReconcileTests.
/// </summary>
public class MediaSyncTests : IAsyncLifetime
{
    private const string AudioWs = "en-Zxxx-x-audio";
    private SyncFixture _fixture = null!;

    public async Task InitializeAsync()
    {
        _fixture = SyncFixture.Create();
        await _fixture.InitializeAsync();
        // Import copies this audio writing system into the CRDT, so both sides can carry an audio-WS value.
        await _fixture.FwDataApi.CreateWritingSystem(new WritingSystem
        {
            Id = Guid.NewGuid(),
            WsId = AudioWs,
            Name = "English Audio",
            Abbreviation = "EN (A)",
            Font = "Arial",
            Type = WritingSystemType.Vernacular
        });
    }

    public async Task DisposeAsync()
    {
        _fixture.DeleteSyncSnapshot();
        await _fixture.DisposeAsync();
    }

    /// <summary>
    /// The id LocalMediaAdapter assigns to <paramref name="fileName"/> under the project's
    /// LinkedFiles/AudioVisual folder, paired with the full path. The id is derived from the path, so
    /// creating the file at that path makes the reference resolvable under the same id.
    /// </summary>
    private (Guid fileId, string fullPath) AudioReference(string fileName)
    {
        var fullPath = Path.Combine(
            _fixture.FwDataApi.Cache.LangProject.LinkedFilesRootDir,
            FwDataMiniLcmApi.AudioVisualFolder,
            fileName);
        return (LocalMediaAdapter.NewGuidV5(fullPath), fullPath);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Sync_RemoveResolvedAudioInFwData_RemovesItFromCrdt()
    {
        // A RESOLVABLE audio reference (file present) that syncs to both sides, then is deleted by a user in
        // FLEx. That FwData-side removal must propagate to the CRDT.
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _fixture.SyncService.Import(crdtApi, fwdataApi);
        var snapshot = await _fixture.RegenerateAndGetSnapshot();

        // Make the audio resolvable: write the file, then register it so it resolves under the same id.
        var (fileId, fullPath) = AudioReference("remove-in-fw.wav");
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, "audio-bytes");
        var mediaAdapter = _fixture.Services.GetRequiredService<IMediaAdapter>();
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
        await _fixture.SyncService.Sync(crdtApi, fwdataApi, snapshot);
        (await fwdataApi.GetEntry(entryId))!.CitationForm[AudioWs].Should().Be(mediaUri.ToString(),
            "guard: the resolvable audio must actually sync to FwData first");
        (await crdtApi.GetEntry(entryId))!.CitationForm[AudioWs].Should().Be(mediaUri.ToString(),
            "guard: the audio must be present in the CRDT before the FwData-side removal");

        // The synced baseline (audio present on both sides) is the ancestor for the next sync.
        var syncedSnapshot = await _fixture.RegenerateAndGetSnapshot();

        // A FLEx user removes the audio reference on the FwData side.
        var fwBefore = await fwdataApi.GetEntry(entryId);
        var fwAfter = fwBefore!.Copy();
        fwAfter.CitationForm.Remove(AudioWs);
        await fwdataApi.UpdateEntry(fwBefore, fwAfter);
        (await fwdataApi.GetEntry(entryId))!.CitationForm.Values.Should().NotContainKey(AudioWs,
            "guard: the audio must actually be removed in FwData before syncing");

        // Sync: the FwData-side removal must propagate to the CRDT.
        await _fixture.SyncService.Sync(crdtApi, fwdataApi, syncedSnapshot);

        using (new AssertionScope())
        {
            (await crdtApi.GetEntry(entryId))!.CitationForm.Values.Should().NotContainKey(AudioWs,
                "a genuine FwData-side audio removal must be propagated to the CRDT, not suppressed");
            // Even worse than failing to propagate: if the CRDT still held the reference, the reverse
            // direction of the same sync would push it back to FwData, resurrecting the deleted reference.
            (await fwdataApi.GetEntry(entryId))!.CitationForm.Values.Should().NotContainKey(AudioWs,
                "the FwData-side removal must not be resurrected by the sync pushing the CRDT value back");
        }
    }
}
