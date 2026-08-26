using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LexCore.Sync;
using SIL.Harmony.Core;

namespace Testing.FwHeadless;

/// <summary>
/// Production-faithful FwHeadless integration repro for the "unresolved media reference" sync crash
/// (wayfinder map "Reproduce the media file-not-found sync crash", ticket 12 — scenario 2: a media id
/// referenced from a CRDT entry whose binary was never uploaded, so no lexbox <c>Files</c> row exists).
///
/// Unlike the in-process <c>MediaSyncTests</c> (ticket 06, which drives <c>LocalMediaAdapter</c>), this runs
/// through the REAL server path: <c>SyncHostedService</c>/<c>SyncWorker</c> orchestration →
/// <c>CrdtFwdataProjectSyncService</c> → <c>LexboxFwDataMediaAdapter</c>, which resolves media purely by
/// <c>FileId</c> against the lexbox <c>Files</c> DB (authority is ignored server-side). Today the CRDT→FwData
/// write hits <c>FromMediaUri</c> for the unresolved id → <c>NotFoundException</c> → <c>SyncObjectException</c>,
/// which <c>SyncHostedService</c> catches and reports as a <see cref="SyncJobStatusEnum.UnknownError"/>
/// <see cref="SyncJobResult"/> (Error contains "File ID:").
///
/// The assertions below encode the DECIDED post-fix behaviour, so they are RED until BOTH fix threads land:
///   - thread A (tickets 04/06): skip the unresolved field instead of crashing, and don't let the snapshot
///     round-trip revert the still-pending reference; and
///   - thread B (tickets 08/09): the reconcile in <c>MediaFileService.SyncMediaFiles(projectId, ...)</c> must
///     not delete a not-yet-uploaded (RemoteId == null) Harmony resource before the write.
/// Each assertion is commented "BUG TODAY" (current observed behaviour) vs "DESIRED" (asserted, red until fix).
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
        var fileId = Guid.NewGuid(); // real, non-empty id with NO Files row -> unresolvable when writing to FwData
        await InjectCrdtCommits(ProjectId,
            AudioWritingSystemChange(wsId),
            UnresolvedAudioEntryChange(entryId, fileId));

        await FwHeadlessTestHelpers.TriggerSync(HttpClient, ProjectId);
        var result = await FwHeadlessTestHelpers.AwaitSyncResult(HttpClient, ProjectId);
        result.Should().NotBeNull();

        // BUG TODAY: the CRDT→FwData write applies the entry, SetString hits the audio WS value, FromMediaUri
        // resolves the id against the Files DB -> null -> NotFoundException -> SyncObjectException. SyncHostedService
        // reports result.Status == UnknownError with result.Error containing "File ID: {fileId}".
        // DESIRED after the thread-A fix (tickets 04/06): the unresolved audio field is skipped and the rest of the
        // entry syncs, so the whole job succeeds. RED until the fix lands.
        result!.Status.Should().Be(SyncJobStatusEnum.Success,
            "the unresolved audio reference must be skipped rather than crash the whole sync job " +
            "(RED until the thread-A skip-field fix lands; today this is UnknownError, Error: {0})",
            result.Error);
    }

    [Fact]
    public async Task Sync_UnresolvedMediaReference_HealsAfterBinaryUpload()
    {
        // Inject the full, faithful scenario: audio WS + entry referencing the id + the matching not-yet-uploaded
        // Harmony resource (RemoteId == null). The pending resource is what makes the scenario production-faithful
        // and exercises the thread-B reconcile that deletes it before the write. NOTE (ticket 11 gap 4): the direct
        // "pending resource survived reconcile" assertion is owned by MediaFileServiceTests (RequiresDb, ticket 10),
        // not here — over HTTP there is no way to observe Harmony resource survival, so this test asserts thread-A/B
        // convergence only indirectly, via the end-to-end heal round-trip below.
        var wsId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        await InjectCrdtCommits(ProjectId,
            AudioWritingSystemChange(wsId),
            UnresolvedAudioEntryChange(entryId, fileId),
            PendingUploadResourceChange(fileId));

        // Second sync: BUG TODAY it crashes (UnknownError, "File ID:"); DESIRED after thread-A it skips + succeeds,
        // leaving the reference pending in the CRDT so a later sync can heal it. RED until the fix lands.
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, ProjectId);
        var skipResult = await FwHeadlessTestHelpers.AwaitSyncResult(HttpClient, ProjectId);
        skipResult.Should().NotBeNull();
        skipResult!.Status.Should().Be(SyncJobStatusEnum.Success,
            "the unresolved audio reference must be skipped, not crash the sync " +
            "(RED until the thread-A skip-field fix; today this is UnknownError, Error: {0})",
            skipResult.Error);

        // Heal: upload the binary for the exact referenced id (creates the Files row under AudioVisual).
        var healUpload = await HealUploadAudio(fileId);
        healUpload.IsSuccessStatusCode.Should().BeTrue("uploading the referenced media binary must succeed");

        // Re-sync: DESIRED the previously-skipped reference now resolves and the audio writes to FwData, so the
        // job succeeds. This end-to-end heal is the indirect proxy for thread-A/B convergence: it requires the
        // thread-A fix (skip + don't revert the pending reference via the snapshot round-trip) AND a live heal
        // path preserved through reconcile (thread B). RED until both fixes land.
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, ProjectId);
        var healResult = await FwHeadlessTestHelpers.AwaitSyncResult(HttpClient, ProjectId);
        healResult.Should().NotBeNull();
        healResult!.Status.Should().Be(SyncJobStatusEnum.Success,
            "once the binary is uploaded the previously-skipped audio reference must heal and the sync succeed " +
            "(RED until both fix threads land; today Error: {0})",
            healResult.Error);
    }
}
