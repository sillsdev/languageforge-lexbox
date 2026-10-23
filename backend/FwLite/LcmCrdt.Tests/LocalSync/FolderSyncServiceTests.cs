using FwLiteShared.LocalSync;
using LcmCrdt.Data;
using LcmCrdt.LocalSync;
using Microsoft.Extensions.Logging.Abstractions;
using SIL.Harmony.Core;
using static LcmCrdt.Tests.LocalSync.LocalSyncTestHelpers;

namespace LcmCrdt.Tests.LocalSync;

public class FolderSyncServiceTests : IAsyncLifetime
{
    private static readonly Guid ProjectId = Guid.Parse("9c1e3a5b-7d2f-4b6e-8a0c-2e4f6b8d0a35");
    private readonly MiniLcmApiFixture _a = CreateFixture(ProjectId);
    private readonly MiniLcmApiFixture _b = CreateFixture(ProjectId, seedWs: false);
    private readonly TempFolder _folder = new();

    public async Task InitializeAsync()
    {
        await _a.InitializeAsync();
        await _b.InitializeAsync();
        await MakeEditor(_a, "user-a");
        await MakeEditor(_b, "user-b");
        // b starts as a copy of a's project
        await _b.DataModel.SyncWith(_a.DataModel);
    }

    public async Task DisposeAsync()
    {
        await _b.DisposeAsync();
        await _a.DisposeAsync();
        _folder.Dispose();
    }

    private static async Task<FolderSyncResult> SyncWithFolder(MiniLcmApiFixture fixture, string path) =>
        await fixture.GetService<FolderSyncService>().SyncWithFolder(path);

    private SyncFolderManifest ManifestForA() =>
        new(ProjectId, "sena-3", "Sena 3", null, ["en"]);

    private string[] FolderFiles() =>
        [.. Directory.EnumerateFiles(_folder.Path).Select(Path.GetFileName).OfType<string>()];

    [Fact]
    public async Task SyncingWithAnEmptyFolder_WritesTheProjectIntoIt()
    {
        var result = await SyncWithFolder(_a, _folder.Path);

        result.Synced.Should().BeTrue();
        var manifest = SyncFolder.Inspect(_folder.Path).Should().BeOfType<SyncFolderState.HoldsProject>().Which.Manifest;
        manifest.ProjectId.Should().Be(ProjectId);
        manifest.ProjectCode.Should().Be("sena-3");
        manifest.WritingSystemIds.Should().BeEquivalentTo(["en"]);
        FolderFiles().Should().Contain(f => f.StartsWith("client_") && f.EndsWith(".jsonl"));
    }

    [Fact]
    public async Task ChangesTravelBetweenTwoCopiesThroughTheFolder()
    {
        var apple = await _a.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "apple" } });
        await SyncWithFolder(_a, _folder.Path);
        var banana = await _b.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "banana" } });

        (await SyncWithFolder(_b, _folder.Path)).Synced.Should().BeTrue();
        (await SyncWithFolder(_a, _folder.Path)).Synced.Should().BeTrue();

        (await _b.Api.GetEntry(apple.Id)).Should().NotBeNull();
        (await _a.Api.GetEntry(banana.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task AFolderHoldingAnotherProject_IsLeftAlone()
    {
        var otherProject = new SyncFolderManifest(Guid.Parse("4d6f8a0b-2c4e-4f6a-8b0d-3e5f7a9c1b46"), "malango", "Malango", null, ["mal"]);
        SyncFolder.WriteManifest(_folder.Path, otherProject);

        var result = await SyncWithFolder(_a, _folder.Path);

        result.Synced.Should().BeFalse();
        result.Sync!.Comparison.Should().Be(IdentityComparison.DifferentProject);
        result.FolderState.Should().BeOfType<SyncFolderState.HoldsProject>().Which.Manifest.ProjectName.Should().Be("Malango");
        FolderFiles().Should().BeEquivalentTo(["manifest.json"]);
    }

    [Fact]
    public async Task AFolderOfUnrelatedFiles_IsLeftAlone()
    {
        File.WriteAllText(Path.Combine(_folder.Path, "notes.txt"), "not a project");

        var result = await SyncWithFolder(_a, _folder.Path);

        result.Synced.Should().BeFalse();
        result.FolderState.Should().BeOfType<SyncFolderState.NotASyncFolder>();
        FolderFiles().Should().BeEquivalentTo(["notes.txt"]);
    }

    [Fact]
    public async Task AFolderWhoseCommitsHaveADifferentHistory_IsRefusedEvenThoughItsManifestMatches()
    {
        // same project Id, created independently: the manifest alone would say it's our project
        await using var impostor = CreateFixture(ProjectId);
        await impostor.InitializeAsync();
        await MakeEditor(impostor, "impostor-user");
        var cherry = await impostor.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "cherry" } });
        await SyncWithFolder(impostor, _folder.Path);

        var result = await SyncWithFolder(_a, _folder.Path);

        result.Sync!.Comparison.Should().Be(IdentityComparison.DifferentRootCommit);
        result.Synced.Should().BeFalse();
        (await _a.Api.GetEntry(cherry.Id)).Should().BeNull();
    }

    [Fact]
    public async Task AfterASync_TheFolderIsRememberedForTheProject()
    {
        var service = _a.GetService<FolderSyncService>();
        service.GetRememberedFolder().Should().BeNull();

        await service.SyncWithFolder(_folder.Path);

        service.GetRememberedFolder().Should().Be(_folder.Path);
    }

    [Fact]
    public async Task AFolderWithOnlyOurManifest_CanBeSynced()
    {
        // an export interrupted after the manifest was written but before any commits
        SyncFolder.WriteManifest(_folder.Path, ManifestForA());

        var result = await SyncWithFolder(_a, _folder.Path);

        result.Synced.Should().BeTrue();
    }

    [Fact]
    public async Task AFolderWithAPartlyWrittenCommitLog_CanBeSynced()
    {
        // an export interrupted part way: some of our commits made it to the drive, but not the root commit
        var options = JsonOptions(_a);
        var ours = (await _a.DataModel.GetChanges(new SyncState([]))).MissingFromClient.DefaultOrder().ToArray();
        SyncFolder.WriteManifest(_folder.Path, ManifestForA());
        await new JsonSyncable(new DirectoryInfo(_folder.Path), options, NullLogger.Instance)
            .AddRangeFromSync(JsonRoundTrip(ours[1..], options));

        var result = await SyncWithFolder(_a, _folder.Path);

        result.Synced.Should().BeTrue();
    }

    [Fact]
    public async Task CommitsFromTheFolder_DoNotLookUploadedToLexboxOnTheReceivingDevice()
    {
        await _a.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "apple" } });
        // a synced with Lexbox before exporting, so its commits carry a Lexbox sync date
        await _a.GetService<SyncRepository>().UpdateSyncDate(DateTimeOffset.UtcNow);
        await SyncWithFolder(_a, _folder.Path);

        await SyncWithFolder(_b, _folder.Path);

        (await _b.GetService<SyncRepository>().GetLatestSyncedCommitDate()).Should().BeNull("b has never synced with Lexbox");
    }
}
