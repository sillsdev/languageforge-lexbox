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
/// A file created in FwLite but not yet uploaded also has no <c>Files</c> row, so today the reconcile
/// destroys that pending resource. Because <see cref="LcmMediaService.DeleteResource"/> emits a *synced*
/// <c>DeleteRemoteResourceChange</c>, the deletion propagates back to the client that still physically
/// holds the un-uploaded bytes and permanently removes it from the client's automatic re-upload set —
/// silent, effectively-permanent data loss.
///
/// The decided fix (ticket 09) guards the delete with <c>if (!lcmResource.Remote) continue;</c> so that:
///   - a resource with <c>RemoteId == null</c> (<c>Remote == false</c>, never uploaded / pending) SURVIVES, and
///   - a resource with <c>RemoteId != null</c> (<c>Remote == true</c>) but no <c>Files</c> row (a genuine
///     orphan left behind after the file was removed on the server) is STILL deleted.
///
/// <see cref="SyncMediaFiles_PreservesResourceThatWasNeverUploaded"/> and
/// <see cref="SyncMediaFiles_PreservesPendingButStillDeletesOrphan"/> encode the DESIRED post-fix behavior
/// and are therefore RED until the guard lands (they demonstrate the bug today). These are RequiresDb /
/// CI-only tests; do not run them locally without the lexbox stack.
/// </summary>
[Collection(nameof(TestingServicesFixture))]
[Trait("Category", "RequiresDb")]
public class SyncMediaFilesReconcileTests : IAsyncLifetime
{
    private readonly MediaFileService _service;
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
        _service = new MediaFileService(services.GetDbContext(), config, services.GetRequiredService<ISendReceiveService>());

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

    /// <summary>Seed a not-yet-uploaded (pending) resource: RemoteId == null, so Remote == false.</summary>
    private async Task<Guid> SeedPendingResource()
    {
        var id = Guid.NewGuid();
        await _dataModel.AddChange(_clientId, new CreateRemoteResourcePendingUploadChange<LcmFileMetadata>(id));
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
    public async Task SyncMediaFiles_PreservesResourceThatWasNeverUploaded()
    {
        // RED until the ticket 09 fix lands: today the reconcile deletes this pending resource.
        var pendingId = await SeedPendingResource();

        var before = await CurrentResources();
        before.Should().ContainSingle(r => r.Id == pendingId)
            .Which.Remote.Should().BeFalse("a not-yet-uploaded resource has a null RemoteId");

        await _service.SyncMediaFiles(_projectId, _lcmMediaService);

        var after = await CurrentResources();
        after.Select(r => r.Id).Should().Contain(pendingId,
            "a not-yet-uploaded resource (RemoteId == null) has no Files row yet, but deleting it emits a synced " +
            "DeleteRemoteResourceChange that permanently kills the client's automatic re-upload; it must survive reconcile");
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
    public async Task SyncMediaFiles_PreservesPendingButStillDeletesOrphan()
    {
        // RED until the fix: proves the guard is selective — it preserves pending resources without
        // disabling orphan cleanup wholesale, both observed in a single reconcile pass.
        var pendingId = await SeedPendingResource();
        var orphanId = await SeedUploadedResource();

        var before = (await CurrentResources()).Select(r => r.Id).ToArray();
        before.Should().Contain(pendingId).And.Contain(orphanId);

        await _service.SyncMediaFiles(_projectId, _lcmMediaService);

        var after = (await CurrentResources()).Select(r => r.Id).ToArray();
        after.Should().NotContain(orphanId, "the uploaded resource with no Files row is a genuine orphan and is cleaned up");
        after.Should().Contain(pendingId, "the not-yet-uploaded resource must survive so its reference can heal after a later upload");
    }

    private sealed class OfflineHttpClientProvider : IServerHttpClientProvider
    {
        public ValueTask<HttpClient> GetHttpClient() => throw new NotImplementedException();

        public ValueTask<ConnectionStatus> ConnectionStatus(bool forceRefresh = false) =>
            ValueTask.FromResult(MiniLcm.Project.ConnectionStatus.Offline);
    }
}
