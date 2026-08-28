using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LexCore.Sync;
using MiniLcm.Media;
using SIL.Harmony.Core;

namespace Testing.FwHeadless;

/// <summary>
/// Production-faithful FwHeadless integration repro for the "unresolved media reference" sync crash
/// (wayfinder map "Reproduce the media file-not-found sync crash", ticket 12 — scenario 2: a media id
/// referenced from a CRDT entry whose binary was never uploaded, so no lexbox <c>Files</c> row exists).
///
/// Unlike the in-process <c>MediaSyncTests</c> (which drives <c>LocalMediaAdapter</c>), this runs through the
/// REAL server path: <c>SyncHostedService</c>/<c>SyncWorker</c> orchestration → <c>CrdtFwdataProjectSyncService</c>
/// → <c>LexboxFwDataMediaAdapter</c>, which resolves media purely by <c>FileId</c> against the lexbox
/// <c>Files</c> DB (authority is ignored server-side).
///
/// Under Option D the root-cause fix lives at the media layer: when the harmony reconcile
/// (<c>MediaFileService.SyncMediaFiles(projectId, ...)</c>) meets a not-yet-uploaded Harmony resource that
/// carries usable metadata, it CREATES a pending <c>Files</c> row reserving the anticipated path. The media
/// reference then RESOLVES, so the CRDT→FwData write records the anticipated path into FwData (FieldWorks
/// tolerates the dangling link), and uploading the binary to that same path later self-heals it — no
/// sync-layer special-casing. A bare unresolved reference with no Harmony resource (nothing to reserve a row
/// from) is still skipped on write by the retained <c>FromMediaUri</c>/<c>SetString</c> guard rather than
/// crashing. Either way the sync job reports <see cref="SyncJobStatusEnum.Success"/> instead of a
/// <see cref="SyncJobStatusEnum.UnknownError"/> <see cref="SyncJobResult"/> (Error containing "File ID:").
///
/// CI-only: <c>[Trait("Category","Integration")]</c> needs the full lexbox stack (LexBoxApi + FwHeadless + hg
/// + Postgres) and does not run locally. <c>[Collection("FwHeadless Sync")]</c> serialises it against the
/// other sync tests, since FwHeadless drains its sync queue one project at a time. Derives from
/// <see cref="MediaFileTestFixture"/> to reuse the sena-3 project setup + first (import) sync in
/// <c>InitializeAsync</c> and the media upload helper for the heal phase.
/// </summary>
[Trait("Category", "Integration")]
[Collection("FwHeadless Sync")]
public class UnresolvedMediaSyncTests : MediaFileTestFixture
{
    // sena-3 has no audio writing system, so the repro must create one. Any IETF tag with the Zxxx script +
    // `audio` private-use variant is an audio WS (WritingSystemId.IsAudio); matches MediaSyncTests.
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

    // SCENARIO 1: an entry whose citation form (audio WS) is the not-found SENTINEL
    // (MediaUri.NotFoundString == "sil-media://not-found/00000000-0000-0000-0000-000000000000", FileId == Guid.Empty).
    // Distinct from scenario 2 above, which uses a real, non-empty Guid. Per research 01/03 the sentinel only
    // originates from a FwData→CRDT import read of an out-of-tree file (no FwLite user action mints it, and the
    // original path is discarded and unrecoverable — MediaUri.NotFound carries no query/fragment). Injecting the
    // sentinel value straight into the CreateEntryChange faithfully represents the CRDT state such an import
    // produces — the sentinel is the only thing that survives that read — and is the scenario-1 analogue of how
    // the ticket-15 unit repro directly sets the offending string. There is NO Files row and NO Harmony resource
    // (a sentinel has no id to key on), which is exactly why scenario 1 has no heal phase.
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
    // This is what the thread-B reconcile deletes today, one step before the CRDT→FwData write.
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
        // The first (import) sync already ran in InitializeAsync, so a ProjectSnapshot exists and the second
        // sync takes the CRDT→FwData Sync path (not Import) — the path the reported crash occurs on.
        var wsId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        // Real, non-empty id with NO Files row AND no Harmony resource: nothing for the reconcile to reserve a
        // pending row from, so this reference stays unresolvable when writing to FwData.
        var fileId = Guid.NewGuid();
        await InjectCrdtCommits(ProjectId,
            AudioWritingSystemChange(wsId),
            UnresolvedAudioEntryChange(entryId, fileId));

        await FwHeadlessTestHelpers.TriggerSync(HttpClient, ProjectId);
        var result = await FwHeadlessTestHelpers.AwaitSyncResult(HttpClient, ProjectId);
        result.Should().NotBeNull();

        // With no Harmony resource there is no metadata to reserve a pending Files row from, so Option D's
        // media-layer heal doesn't apply here. The retained FromMediaUri/SetString guard still resolves the id
        // against the Files DB -> null -> skips the audio field rather than throwing, so the rest of the entry
        // syncs and the whole job succeeds (instead of the pre-fix UnknownError / "File ID: {fileId}" crash).
        result!.Status.Should().Be(SyncJobStatusEnum.Success,
            "an unresolved audio reference with no resource must be skipped rather than crash the whole sync job " +
            "(today Error: {0})",
            result.Error);
    }

    [Fact]
    public async Task Sync_WithNotFoundSentinelMediaReference_SkipsFieldAndSucceeds()
    {
        // SCENARIO 1 (the not-found sentinel). The first (import) sync already ran in InitializeAsync, so a
        // ProjectSnapshot exists and this second sync takes the CRDT→FwData Sync path. The entry does NOT yet
        // exist on the FwData side, so sync classifies it as an add and takes CreateEntry's unguarded
        // UpdateLcmMultiString→SetString→FromMediaUri path — the create path is unguarded, unlike the update
        // path which ShouldSet already protects, so scenario 1 crashes ONLY on create.
        var wsId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        // No fileId, no Files row, no Harmony resource: the sentinel is FileId == Guid.Empty and identity-free.
        await InjectCrdtCommits(ProjectId,
            AudioWritingSystemChange(wsId),
            SentinelAudioEntryChange(entryId));

        await FwHeadlessTestHelpers.TriggerSync(HttpClient, ProjectId);
        var result = await FwHeadlessTestHelpers.AwaitSyncResult(HttpClient, ProjectId);
        result.Should().NotBeNull();

        // The sentinel is identity-free (FileId == Guid.Empty) and has no Harmony resource, so Option D's
        // media-layer heal cannot apply. On the create path FromMediaUri short-circuits the sentinel to null
        // and SetString skips the audio field rather than throwing, so the rest of the entry syncs and the job
        // succeeds (instead of the pre-fix UnknownError / "File ID: 00000000-..." crash).
        result!.Status.Should().Be(SyncJobStatusEnum.Success,
            "the not-found sentinel audio reference must be skipped rather than crash the whole sync job " +
            "(today Error: {0})",
            result.Error);

        // NO heal round-trip here — deliberately unlike scenario 2 (HealsAfterBinaryUpload). The sentinel is an
        // identity-free constant (FileId == Guid.Empty) with no key and no pending binary to heal against, and an
        // out-of-tree file never reaches the server, so there is nothing to POST to /api/media and nothing to
        // re-resolve. The audio stays absent; re-attaching it is a manual user action (ticket 13, Q5 = C).
    }

    [Fact]
    public async Task Sync_UnresolvedMediaReference_HealsAfterBinaryUpload()
    {
        // Inject the full, faithful scenario: audio WS + entry referencing the id + the matching not-yet-uploaded
        // Harmony resource (RemoteId == null) WITH metadata (a filename). Under Option D that metadata is what
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

        // Second sync: Option D's reconcile creates a pending Files row for the resource, so FromMediaUri now
        // resolves the id to the anticipated path and the CRDT→FwData write records that (dangling) path in
        // FwData rather than skipping or crashing. The job succeeds.
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, ProjectId);
        var skipResult = await FwHeadlessTestHelpers.AwaitSyncResult(HttpClient, ProjectId);
        skipResult.Should().NotBeNull();
        skipResult!.Status.Should().Be(SyncJobStatusEnum.Success,
            "the reference resolves via the reserved pending Files row and writes the anticipated path, not crash " +
            "(today Error: {0})",
            skipResult.Error);

        // Heal: upload the binary for the exact referenced id. The upload finds the pending row, keeps its
        // reserved path, writes the binary there and advances its revision to 1 — self-healing the link.
        var healUpload = await HealUploadAudio(fileId);
        healUpload.IsSuccessStatusCode.Should().BeTrue("uploading the referenced media binary must succeed");

        // Re-sync: the binary now exists at the anticipated path, so the reference is fully resolvable on both
        // sides and the job continues to succeed. This end-to-end heal is the indirect proxy for Option D's
        // media-layer fix: it requires the pending-row reservation AND the upload landing at that reserved path.
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, ProjectId);
        var healResult = await FwHeadlessTestHelpers.AwaitSyncResult(HttpClient, ProjectId);
        healResult.Should().NotBeNull();
        healResult!.Status.Should().Be(SyncJobStatusEnum.Success,
            "once the binary is uploaded to the reserved path the audio reference is fully resolvable and the sync succeeds " +
            "(today Error: {0})",
            healResult.Error);
    }
}
