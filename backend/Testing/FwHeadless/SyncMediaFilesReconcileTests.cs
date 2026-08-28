using FwHeadless;
using FwHeadless.Media;
using FwHeadless.Services;
using LcmCrdt;
using LcmCrdt.MediaServer;
using LexData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiniLcm.Media;
using MiniLcm.Project;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Resource;
using Testing.Fixtures;

namespace Testing.FwHeadless;

/// <summary>
/// Reconcile-delete regression tests for <see cref="MediaFileService.SyncMediaFiles(System.Guid, LcmMediaService)"/>.
///
/// The reconcile loop deletes every Harmony CRDT resource that has no matching lexbox <c>Files</c> row.
/// A file created in FwLite but not yet uploaded also has no <c>Files</c> row. Deleting that pending
/// resource is data loss: <see cref="LcmMediaService.DeleteResource"/> emits a *synced*
/// <c>DeleteRemoteResourceChange</c> that propagates back to the client still physically holding the
/// un-uploaded bytes and permanently removes it from the client's automatic re-upload set.
///
/// Under Option D the reconcile no longer merely skips a pending (<c>Remote == false</c>) resource — when
/// the resource carries usable metadata (a non-empty filename) it CREATES a pending <c>Files</c> row that
/// reserves the anticipated path (<c>Revision == 0</c>), so the media reference resolves and the
/// normal sync writes the anticipated path into FwData. So:
///   - a pending resource WITH metadata → a pending <c>Files</c> row is created and the resource SURVIVES, and
///   - a resource with <c>RemoteId != null</c> (<c>Remote == true</c>) but no <c>Files</c> row (a genuine
///     orphan left behind after the file was removed on the server) is STILL deleted.
///
/// These are RequiresDb / CI-only tests; do not run them locally without the lexbox stack.
/// </summary>
[Collection(nameof(TestingServicesFixture))]
[Trait("Category", "RequiresDb")]
public class SyncMediaFilesReconcileTests : IAsyncLifetime
{
    private readonly MediaFileService _service;
    private readonly LexBoxDbContext _dbContext;
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly string _crdtProjectPath;
    private ServiceProvider _crdtServices = null!;
    private AsyncServiceScope _crdtScope;
    private DataModel _dataModel = null!;
    private LcmMediaService _lcmMediaService = null!;
    private Guid _clientId;

    public SyncMediaFilesReconcileTests(TestingServicesFixture testing)
    {
        var services = testing.ConfigureServices(s => s.AddFwHeadless().Configure<FwHeadlessConfig>(config =>
        {
            config.LexboxUrl = "http://localhost/";
            config.LexboxUsername = "admin";
            config.LexboxPassword = "pass";
            config.ProjectStorageRoot = Path.GetFullPath(Path.Combine(".", $"SyncMediaFilesReconcileTests-{Guid.NewGuid():N}"));
            config.MediaFileAuthority = "localhost";
        }));
        var config = services.GetRequiredService<IOptions<FwHeadlessConfig>>();
        Directory.CreateDirectory(config.Value.ProjectStorageRoot);
        // The reconcile overload only reads the lexbox Files table and the Harmony resources; we use a
        // fresh project id so there are no Files rows for the seeded resources (that absence is exactly
        // the condition that triggers the reconcile-delete under test).
        _dbContext = services.GetDbContext();
        _service = new MediaFileService(_dbContext, config, services.GetRequiredService<ISendReceiveService>());

        _crdtProjectPath = Path.GetFullPath(Path.Combine(".", $"SyncMediaFilesReconcileTests-crdt-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(_crdtProjectPath);
    }

    public async Task InitializeAsync()
    {
        _crdtServices = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationManager())
            .AddLogging()
            .AddLcmCrdtClient()
            .AddSingleton<IServerHttpClientProvider, OfflineHttpClientProvider>()
            .Configure<LcmCrdtConfig>(c =>
            {
                c.ProjectPath = _crdtProjectPath;
                c.EnableProjectDataFileCache = false;
            })
            .BuildServiceProvider();

        var projectsService = _crdtServices.GetRequiredService<CrdtProjectsService>();
        var crdtProject = await projectsService.CreateProject(
            new CrdtProjectsService.CreateProjectRequest("Reconcile Test", "reconcile-test", Id: _projectId));

        _crdtScope = _crdtServices.CreateAsyncScope();
        await _crdtScope.ServiceProvider.OpenCrdtProject(crdtProject);
        _dataModel = _crdtScope.ServiceProvider.GetRequiredService<DataModel>();
        _lcmMediaService = _crdtScope.ServiceProvider.GetRequiredService<LcmMediaService>();
        _clientId = _crdtScope.ServiceProvider.GetRequiredService<CurrentProjectService>().ProjectData.ClientId;
    }

    public async Task DisposeAsync()
    {
        await _crdtScope.DisposeAsync();
        await _crdtServices.DisposeAsync();
        SafeDeleteDirectory(_crdtProjectPath);
    }

    private static void SafeDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { /* best effort cleanup */ }
    }

    /// <summary>
    /// Seed a not-yet-uploaded (pending) resource: RemoteId == null, so Remote == false. When
    /// <paramref name="filename"/> is provided the resource also carries metadata (a filename), which is
    /// what lets the Option D reconcile create a pending Files row for it.
    /// </summary>
    private async Task<Guid> SeedPendingResource(string? filename = "pending-upload.wav")
    {
        var id = Guid.NewGuid();
        await _dataModel.AddChange(_clientId, new CreateRemoteResourcePendingUploadChange<LcmFileMetadata>(id));
        if (filename is not null)
        {
            await _dataModel.AddChange(_clientId,
                new SetRemoteResourceMetadataChange<LcmFileMetadata>(id, new LcmFileMetadata(filename, "audio/wav")));
        }
        return id;
    }

    /// <summary>Seed an uploaded resource: RemoteId != null, so Remote == true. With no Files row it is a genuine orphan.</summary>
    private async Task<Guid> SeedUploadedResource()
    {
        var id = Guid.NewGuid();
        await _dataModel.AddChange(_clientId, new CreateRemoteResourceChange<LcmFileMetadata>(id, id.ToString("N")));
        return id;
    }

    private async Task<HarmonyResource<LcmFileMetadata>[]> CurrentResources()
    {
        return await _lcmMediaService.AllResources();
    }

    [Fact]
    public async Task SyncMediaFiles_CreatesPendingFilesRowForNeverUploadedResource()
    {
        // Option D: a not-yet-uploaded resource with usable metadata gets a pending Files row (so its media
        // reference resolves) and the resource itself survives reconcile.
        var pendingId = await SeedPendingResource();

        var before = await CurrentResources();
        before.Should().ContainSingle(r => r.Id == pendingId)
            .Which.Remote.Should().BeFalse("a not-yet-uploaded resource has a null RemoteId");
        _dbContext.Files.Any(f => f.Id == pendingId).Should().BeFalse("guard: no Files row exists before reconcile");

        await _service.SyncMediaFiles(_projectId, _lcmMediaService);

        var after = await CurrentResources();
        after.Select(r => r.Id).Should().Contain(pendingId,
            "the not-yet-uploaded resource must survive reconcile so its reference can heal after a later upload");

        var pendingRow = _dbContext.Files.SingleOrDefault(f => f.Id == pendingId);
        pendingRow.Should().NotBeNull("Option D creates a pending Files row that reserves the anticipated path");
        pendingRow!.Revision.Should().Be(0, "revision 0 marks a reservation for a binary that hasn't been uploaded yet");
        pendingRow.ProjectId.Should().Be(_projectId);
        pendingRow.Filename.Should().EndWith("pending-upload.wav", "the anticipated path embeds the resource's filename");
    }

    [Fact]
    public async Task SyncMediaFiles_LeavesResourceWithoutMetadataUntouched()
    {
        // Without usable metadata (no filename) we can't reserve a path, so the resource is left as-is: no
        // Files row is created and the resource still survives (the old skip behavior).
        var pendingId = await SeedPendingResource(filename: null);

        await _service.SyncMediaFiles(_projectId, _lcmMediaService);

        var after = await CurrentResources();
        after.Select(r => r.Id).Should().Contain(pendingId, "a pending resource is never deleted by reconcile");
        _dbContext.Files.Any(f => f.Id == pendingId).Should().BeFalse(
            "without a filename there is no anticipated path to reserve, so no pending Files row is created");
    }

    [Fact]
    public async Task SyncMediaFiles_DeletesUploadedResourceWithNoDbFile()
    {
        // GREEN today and after the fix: a genuinely orphaned (previously-uploaded) resource is still cleaned up.
        var orphanId = await SeedUploadedResource();

        var before = await CurrentResources();
        before.Should().ContainSingle(r => r.Id == orphanId)
            .Which.Remote.Should().BeTrue("an uploaded resource has a non-null RemoteId");

        await _service.SyncMediaFiles(_projectId, _lcmMediaService);

        var after = await CurrentResources();
        after.Select(r => r.Id).Should().NotContain(orphanId,
            "an uploaded resource (RemoteId != null) with no matching Files row is a genuine orphan and is still deleted by reconcile");
    }

    [Fact]
    public async Task SyncMediaFiles_CreatesPendingRowButStillDeletesOrphan()
    {
        // Proves the reconcile is selective in a single pass: it reserves a pending Files row for the
        // not-yet-uploaded resource (which survives) without disabling orphan cleanup wholesale.
        var pendingId = await SeedPendingResource();
        var orphanId = await SeedUploadedResource();

        var before = (await CurrentResources()).Select(r => r.Id).ToArray();
        before.Should().Contain(pendingId).And.Contain(orphanId);

        await _service.SyncMediaFiles(_projectId, _lcmMediaService);

        var after = (await CurrentResources()).Select(r => r.Id).ToArray();
        after.Should().NotContain(orphanId, "the uploaded resource with no Files row is a genuine orphan and is cleaned up");
        after.Should().Contain(pendingId, "the not-yet-uploaded resource must survive so its reference can heal after a later upload");

        var pendingRow = _dbContext.Files.SingleOrDefault(f => f.Id == pendingId);
        pendingRow.Should().NotBeNull("the pending resource gets a reserved Files row");
        pendingRow!.Revision.Should().Be(0);
        _dbContext.Files.Any(f => f.Id == orphanId).Should().BeFalse("the orphan never had a Files row and none is created");
    }

    [Fact]
    public async Task SyncMediaFiles_ReclaimsPendingRowWhoseResourceIsGone()
    {
        // A pending Files row (Revision == 0) reserves the anticipated path of a not-yet-uploaded resource.
        // If that resource later disappears, the row is orphaned: the pre-guard reconcile would hit
        // AddExistingRemoteResource and throw FileNotFoundException (a pending row has no binary on disk).
        // The guard instead reclaims the orphaned row, tying the reserved row's lifetime to its resource.
        var id = await SeedPendingResource();

        // First reconcile creates the pending reservation via the real create path.
        await _service.SyncMediaFiles(_projectId, _lcmMediaService);
        var pendingRow = _dbContext.Files.SingleOrDefault(f => f.Id == id);
        pendingRow.Should().NotBeNull("the reconcile reserves a pending Files row for the not-yet-uploaded resource");
        pendingRow!.Revision.Should().Be(0, "the reserved row is pending until its binary is uploaded");

        // Step 3 simulates a resource deletion that NO current production path performs — it is what issue
        // #2607's future media GC will do. We delete the Harmony resource ourselves to orphan the pending row.
        await _lcmMediaService.DeleteResource(id);
        (await CurrentResources()).Select(r => r.Id).Should().NotContain(id,
            "we deleted the Harmony resource ourselves to orphan the pending row");

        // Second reconcile must NOT throw: on the pre-guard code the orphaned pending row hits
        // AddExistingRemoteResource -> FileNotFoundException. The guard reclaims it instead (deferring true
        // entry-abandonment GC to #2607).
        await _service.SyncMediaFiles(_projectId, _lcmMediaService);

        _dbContext.Files.Any(f => f.Id == id).Should().BeFalse(
            "the orphaned pending row is reclaimed once its reserving resource is gone");
    }

    private sealed class OfflineHttpClientProvider : IServerHttpClientProvider
    {
        public ValueTask<HttpClient> GetHttpClient() => throw new NotImplementedException();

        public ValueTask<ConnectionStatus> ConnectionStatus(bool forceRefresh = false) =>
            ValueTask.FromResult(MiniLcm.Project.ConnectionStatus.Offline);
    }
}
