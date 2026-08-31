using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LexCore.Sync;
using MiniLcm.Media;
using SIL.Harmony.Core;

namespace Testing.FwHeadless;

/// <summary>
/// Production-faithful FwHeadless integration repro for the "unresolved media reference" sync crash
/// a media id referenced from a CRDT entry whose binary was never uploaded, so no lexbox <c>Files</c> row exists).
///
/// Unlike the in-process <c>MediaSyncTests</c> (which drives <c>LocalMediaAdapter</c>), this runs through the
/// REAL server path: <c>SyncHostedService</c>/<c>SyncWorker</c> orchestration → <c>CrdtFwdataProjectSyncService</c>
/// → <c>LexboxFwDataMediaAdapter</c>, which resolves media purely by <c>FileId</c> against the lexbox
/// <c>Files</c> DB (authority is ignored server-side).
/// </summary>
[Trait("Category", "Integration")]
[Collection("FwHeadless Sync")]
public class UnresolvedMediaSyncTests : MediaFileTestFixture
{
    // sena-3 has no audio writing system, so the repro must create one
    private const string AudioWs = "en-Zxxx-x-audio";

    /// <summary>
    /// Posts raw <see cref="ServerCommit"/>s straight to <c>POST api/crdt/{projectId}/add</c> (no change-type
    /// validation server-side — the payloads are only interpreted later when FwHeadless pulls them), one commit
    /// per change with an increasing HybridDateTime counter for deterministic ordering. Mirrors the raw-JSON
    /// approach of <c>MergeFwDataWithHarmonyTests.AddTestCommit</c>.
    /// </summary>
    private async Task InjectCrdtCommits(Guid projectId, params (Guid entityId, string changeJson)[] changes)
    {
        var now = DateTime.UtcNow;
        var serverCommits = changes.Select((change, index) => new ServerCommit(Guid.NewGuid())
        {
            ChangeEntities =
            [
                new ChangeEntity<ServerJsonChange>
                {
                    Change = JsonSerializer.Deserialize<ServerJsonChange>(change.changeJson)
                             ?? throw new JsonException("unable to deserialize change"),
                    Index = 0,
                    CommitId = Guid.NewGuid(),
                    EntityId = change.entityId
                }
            ],
            ClientId = Guid.NewGuid(),
            ProjectId = projectId,
            HybridDateTime = new HybridDateTime(now, index)
        }).ToArray();

        var result = await HttpClient.PostAsJsonAsync($"api/crdt/{projectId}/add", serverCommits);
        result.EnsureSuccessStatusCode();
    }

    // A vernacular audio WS (citation form is a vernacular field). Type 0 == WritingSystemType.Vernacular.
    private static (Guid entityId, string changeJson) AudioWritingSystemChange(Guid wsId) => (wsId,
        $$"""
          {
            "$type": "CreateWritingSystemChange",
            "WsId": "{{AudioWs}}",
            "Name": "English Audio",
            "Abbreviation": "en-audio",
            "Font": "Arial",
            "Exemplars": [],
            "Type": 0,
            "Order": 1000,
            "EntityId": "{{wsId}}"
          }
          """);

    // An entry whose citation form (audio WS) references a media id with NO Files row. Authority is irrelevant
    // server-side (LexboxFwDataMediaAdapter keys on FileId), `localhost` matches the test MediaFileAuthority.
    private static (Guid entityId, string changeJson) UnresolvedAudioEntryChange(Guid entryId, Guid fileId) => (entryId,
        $$"""
          {
            "$type": "CreateEntryChange",
            "LexemeForm": { "en": "rambuta" },
            "CitationForm": { "{{AudioWs}}": "sil-media://localhost/{{fileId}}" },
            "Note": {},
            "EntityId": "{{entryId}}"
          }
          """);

    // an entry whose citation form (audio WS) is the not-found SENTINEL
    private static (Guid entityId, string changeJson) SentinelAudioEntryChange(Guid entryId) => (entryId,
        $$"""
          {
            "$type": "CreateEntryChange",
            "LexemeForm": { "en": "rambuta" },
            "CitationForm": { "{{AudioWs}}": "{{MediaUri.NotFoundString}}" },
            "Note": {},
            "EntityId": "{{entryId}}"
          }
          """);

    // A not-yet-uploaded (RemoteId == null) Harmony resource for the same id. Exact JSON shape captured from
    // a real serialized change (LcmCrdt.Tests ChangeDeserializationRegressionData.latest.verified.txt).
    private static (Guid entityId, string changeJson) PendingUploadResourceChange(Guid fileId) => (fileId,
        $$"""
          {
            "$type": "create:pendingUpload",
            "Metadata": {
              "Filename": "heal-not-uploaded.wav",
              "MimeType": "audio/wav",
              "Author": null,
              "UploadDate": null,
              "SizeInBytes": null
            },
            "EntityId": "{{fileId}}"
          }
          """);

    /// <summary>
    /// Uploads a binary for the exact referenced media id (POST api/media with a `fileId` form field), the real
    /// "heal" step. An `audio/*` content type makes the server place it under LinkedFiles/AudioVisual, where the
    /// audio reference resolves. Returns the upload response so the caller can assert it succeeded.
    /// </summary>
    private async Task<HttpResponseMessage> HealUploadAudio(Guid fileId)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"heal-{fileId:N}.wav");
        CreateDummyFile(tempFile, 1024);
        try
        {
            var (_, response) = await PostFile(tempFile, contentType: "audio/wav", fileId: fileId);
            return response;
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Sync_WithUnresolvedMediaReference_SkipsFieldAndSucceeds()
    {
        var wsId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        await InjectCrdtCommits(ProjectId,
            AudioWritingSystemChange(wsId),
            UnresolvedAudioEntryChange(entryId, fileId));

        await FwHeadlessTestHelpers.TriggerSync(HttpClient, ProjectId);
        var result = await FwHeadlessTestHelpers.AwaitSyncResult(HttpClient, ProjectId);
        result.Should().NotBeNull();

        // With no Harmony resource there is no metadata to reserve a pending Files row from,
        // The FromMediaUri/SetString guard still resolves the id
        // against the Files DB -> null -> skips the audio field rather than throwing, so the rest of the entry
        // syncs and the whole job succeeds (instead of the pre-fix UnknownError / "File ID: {fileId}" crash).
        result.Status.Should().Be(SyncJobStatusEnum.Success,
            "an unresolved audio reference with no resource must be skipped rather than crash the whole sync job, error {0}",
            result.Error);

        var (metadata, response) = await GetFileMetadata(fileId);
        metadata.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Sync_WithNotFoundSentinelMediaReference_SkipsFieldAndSucceeds()
    {
        var wsId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        // No fileId, no Files row, no Harmony resource: the sentinel is FileId == Guid.Empty and identity-free.
        await InjectCrdtCommits(ProjectId,
            AudioWritingSystemChange(wsId),
            SentinelAudioEntryChange(entryId));

        await FwHeadlessTestHelpers.TriggerSync(HttpClient, ProjectId);
        var result = await FwHeadlessTestHelpers.AwaitSyncResult(HttpClient, ProjectId);
        result.Should().NotBeNull();

        result.Status.Should().Be(SyncJobStatusEnum.Success,
            "the not-found sentinel audio reference must be skipped rather than crash the whole sync job Error: {0}",
            result.Error);
    }

    [Fact]
    public async Task Sync_UnresolvedMediaReference_HealsAfterBinaryUpload()
    {
        // audio WS + entry referencing the id + the matching not-yet-uploaded
        // Harmony resource (RemoteId == null) WITH metadata (a filename). that metadata is what
        // lets the harmony reconcile reserve a pending Files row for the resource, so the reference resolves.
        // NOTE: the direct "pending Files row was created / survived reconcile" assertion is owned by
        // SyncMediaFilesReconcileTests (RequiresDb) — over HTTP there is no way to observe the Files table, so
        // this test asserts Option D's convergence only indirectly, via the end-to-end heal round-trip below.
        var wsId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        await InjectCrdtCommits(ProjectId,
            AudioWritingSystemChange(wsId),
            UnresolvedAudioEntryChange(entryId, fileId),
            PendingUploadResourceChange(fileId));

        // Second sync: creates a pending Files row for the resource, so FromMediaUri now
        // resolves the id to the anticipated path and the CRDT→FwData write records that (dangling) path in
        // FwData rather than skipping or crashing. The job succeeds.
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, ProjectId);
        var skipResult = await FwHeadlessTestHelpers.AwaitSyncResult(HttpClient, ProjectId);
        skipResult.Should().NotBeNull();
        skipResult.Status.Should().Be(SyncJobStatusEnum.Success,
            "the reference resolves via the reserved pending Files row and writes the anticipated path, not crash Error: {0}",
            skipResult.Error);
        var (metadata, response) = await GetFileMetadata(fileId);
        metadata.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        metadata.Filename.Should().Be("heal-not-uploaded.wav");
        metadata.MimeType.Should().Be("audio/wav");

        // Heal: upload the binary for the exact referenced id. The upload finds the pending row, keeps its
        // reserved path, writes the binary there and advances its revision to 1 — self-healing the link.
        var healUpload = await HealUploadAudio(fileId);
        healUpload.IsSuccessStatusCode.Should().BeTrue("uploading the referenced media binary must succeed");

        // Re-sync: the binary now exists at the anticipated path, so the reference is fully resolvable on both
        // sides and the job continues to succeed.
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, ProjectId);
        var healResult = await FwHeadlessTestHelpers.AwaitSyncResult(HttpClient, ProjectId);
        healResult.Should().NotBeNull();
        healResult.Status.Should().Be(SyncJobStatusEnum.Success,
            "once the binary is uploaded to the reserved path the audio reference is fully resolvable and the sync succeeds Error: {0}",
            healResult.Error);

        var downloadResponse = await DownloadFile(fileId);
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var downloadContent = await downloadResponse.Content.ReadAsStringAsync();
        downloadContent.Should().NotBeNullOrWhiteSpace();
    }
}
