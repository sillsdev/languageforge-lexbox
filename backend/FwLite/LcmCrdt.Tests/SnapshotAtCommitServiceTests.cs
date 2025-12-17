using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;
using SIL.Harmony.Db;
using SIL.Harmony;
using Microsoft.Data.Sqlite;

namespace LcmCrdt.Tests;

public class SnapshotAtCommitServiceTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    private static readonly AutoFaker AutoFaker = new(AutoFakerDefault.MakeConfig(["en"]));

    private ICrdtDbContext DbContext => fixture.DbContext as ICrdtDbContext ?? throw new InvalidOperationException("DbContext is not ICrdtDbContext");
    private IMiniLcmApi Api => fixture.Api;
    private SnapshotAtCommitService Service => fixture.GetService<SnapshotAtCommitService>();

    private async Task<Commit> GetLatestCommit()
    {
        return await DbContext.Commits.OrderByDescending(c => c.HybridDateTime.DateTime).FirstAsync();
    }

    private void AssertSnapshotsAreEquivalentEqual(ProjectSnapshot expected, ProjectSnapshot actual)
    {
        actual.Should().BeEquivalentTo(expected, options => options
            .WithStrictOrdering()
            .WithoutStrictOrderingFor(x => x.PartsOfSpeech)
            .WithoutStrictOrderingFor(x => x.Publications)
            .WithoutStrictOrderingFor(x => x.SemanticDomains)
            .WithoutStrictOrderingFor(x => x.ComplexFormTypes)
        );
    }

    [Fact]
    public async Task CanRegenerateSnapshotAtPreviousCommit()
    {
        // Arrange: Create entry 1 and capture expected snapshot
        var entry1 = await AutoFaker.EntryReadyForCreation(Api);
        await Api.CreateEntry(entry1);

        var commit1 = await GetLatestCommit();
        var expectedSnapshot1 = await Api.TakeProjectSnapshot();

        // Act: Create entry 2
        var entry2 = await AutoFaker.EntryReadyForCreation(Api);
        await Api.CreateEntry(entry2);

        var commit2 = await GetLatestCommit();
        commit2.Id.Should().NotBe(commit1.Id);

        // Get snapshot at commit 1
        var actualSnapshot1 = await Service.GetProjectSnapshotAtCommit(commit1.Id);

        // Verify: Snapshot at commit 1 should match expected, and current snapshot should have both entries
        actualSnapshot1.Should().NotBeNull();
        AssertSnapshotsAreEquivalentEqual(expectedSnapshot1, actualSnapshot1);

        var currentSnapshot = await Api.TakeProjectSnapshot();
        currentSnapshot.Entries.Should().Contain(e => e.Id == entry1.Id);
        currentSnapshot.Entries.Should().Contain(e => e.Id == entry2.Id);

        // Verify original DB is unchanged
        await fixture.DataModel.RegenerateSnapshots();
        var latestCommit = await GetLatestCommit();
        latestCommit.Id.Should().Be(commit2.Id);
    }

    [Fact]
    public async Task ModifiedEntry_ShowsOriginalState()
    {
        // Arrange: Create entry and capture expected snapshot
        var entryBefore = await AutoFaker.EntryReadyForCreation(Api);
        await Api.CreateEntry(entryBefore);

        var commitBefore = await GetLatestCommit();
        var expectedSnapshot = await Api.TakeProjectSnapshot();

        // Act: Modify the entry
        var entryAfter = entryBefore.Copy();
        entryAfter.LexemeForm["en"] = "modified-lexeme";
        if (entryAfter.Senses.Count > 0)
        {
            entryAfter.Senses[0].Definition["en"] = new("modified-definition");
        }
        await Api.UpdateEntry(entryBefore, entryAfter);

        // Get snapshot at original commit
        var actualSnapshot = await Service.GetProjectSnapshotAtCommit(commitBefore.Id);

        // Verify: Snapshot should show original values
        actualSnapshot.Should().NotBeNull();
        AssertSnapshotsAreEquivalentEqual(expectedSnapshot, actualSnapshot!);

        var actualEntry = actualSnapshot.Entries.First(e => e.Id == entryBefore.Id);
        actualEntry.LexemeForm["en"].Should().Be(entryBefore.LexemeForm["en"]);
        if (entryBefore.Senses.Count > 0)
        {
            actualEntry.Senses[0].Definition["en"].Should().Be(entryBefore.Senses[0].Definition["en"]);
        }

        // Verify original DB still has the modification
        await fixture.DataModel.RegenerateSnapshots();
        var currentEntry = await Api.GetEntry(entryBefore.Id);
        currentEntry!.LexemeForm["en"].Should().Be("modified-lexeme");
    }

    [Fact]
    public async Task DeletedEntry_StillAppearsInSnapshot()
    {
        // Arrange: Create entry and capture expected snapshot
        var entry = await AutoFaker.EntryReadyForCreation(Api);
        await Api.CreateEntry(entry);

        var commitBefore = await GetLatestCommit();
        var expectedSnapshot = await Api.TakeProjectSnapshot();

        // Act: Delete the entry
        await Api.DeleteEntry(entry.Id);

        // Get snapshot at original commit
        var actualSnapshot = await Service.GetProjectSnapshotAtCommit(commitBefore.Id);

        // Verify: Snapshot should still contain the entry
        actualSnapshot.Should().NotBeNull();
        AssertSnapshotsAreEquivalentEqual(expectedSnapshot, actualSnapshot);
        actualSnapshot.Entries.Should().Contain(e => e.Id == entry.Id);

        // Verify original DB no longer has the entry
        await fixture.DataModel.RegenerateSnapshots();
        var currentEntry = await Api.GetEntry(entry.Id);
        currentEntry.Should().BeNull();
    }

    [Fact]
    public async Task NonExistentCommit_ReturnsNull()
    {
        // Arrange: Create at least one entry so we have a valid project state
        var entry = await AutoFaker.EntryReadyForCreation(Api);
        await Api.CreateEntry(entry);

        // Act: Get snapshot at non-existent commit
        var snapshot = await Service.GetProjectSnapshotAtCommit(Guid.NewGuid());

        // Verify: Should return null
        snapshot.Should().BeNull();
    }

    [Fact]
    public async Task LatestCommit_ReturnsCurrentState()
    {
        // Arrange: Create entries
        var entry1 = await AutoFaker.EntryReadyForCreation(Api);
        await Api.CreateEntry(entry1);

        var entry2 = await AutoFaker.EntryReadyForCreation(Api);
        await Api.CreateEntry(entry2);

        var latestCommit = await GetLatestCommit();
        var expectedSnapshot = await Api.TakeProjectSnapshot();

        // Act: Get snapshot at latest commit
        var actualSnapshot = await Service.GetProjectSnapshotAtCommit(latestCommit.Id);

        // Verify: Should match current state
        actualSnapshot.Should().NotBeNull();
        AssertSnapshotsAreEquivalentEqual(expectedSnapshot, actualSnapshot);
    }

    [Fact]
    public async Task ComplexForms_RolledBackCorrectly()
    {
        // Arrange: Create component and complex form
        var componentBefore = await AutoFaker.EntryReadyForCreation(Api);
        await Api.CreateEntry(componentBefore);

        var complexFormBefore = await AutoFaker.EntryReadyForCreation(Api);
        await Api.CreateEntry(complexFormBefore);

        var commitBefore = await GetLatestCommit();
        var expectedSnapshot = await Api.TakeProjectSnapshot();

        // Act: Add complex form relationship
        var componentAfter = componentBefore.Copy();
        var complexFormAfter = complexFormBefore.Copy();
        var cfc = ComplexFormComponent.FromEntries(complexFormAfter, componentAfter);
        componentAfter.ComplexForms.Add(cfc);
        complexFormAfter.Components.Add(cfc);

        await Api.UpdateEntry(componentBefore, componentAfter);
        await Api.UpdateEntry(complexFormBefore, complexFormAfter);

        // Get snapshot at original commit
        var actualSnapshot = await Service.GetProjectSnapshotAtCommit(commitBefore.Id);

        // Verify: Snapshot should match the state before complex form was added
        actualSnapshot.Should().NotBeNull();
        AssertSnapshotsAreEquivalentEqual(expectedSnapshot, actualSnapshot!);

        var actualComponent = actualSnapshot.Entries.First(e => e.Id == componentBefore.Id);
        actualComponent.ComplexForms.Should().NotBeEmpty();
        actualComponent.ComplexForms.FirstOrDefault(cf => cf.ComplexFormEntryId == complexFormAfter.Id).Should().BeNull();

        var actualComplexForm = actualSnapshot.Entries.First(e => e.Id == complexFormBefore.Id);
        actualComplexForm.Components.Should().NotBeEmpty();
        actualComplexForm.Components.FirstOrDefault(c => c.ComponentEntryId == componentAfter.Id).Should().BeNull();

        // Verify current DB has the relationship
        await fixture.DataModel.RegenerateSnapshots();
        var currentComponent = await Api.GetEntry(componentBefore.Id);
        currentComponent!.ComplexForms.FirstOrDefault(cf => cf.ComplexFormEntryId == complexFormAfter.Id).Should().NotBeNull();
    }
}

/// <summary>
/// File-based database tests for SnapshotAtCommitService.
/// These tests use actual file-based SQLite databases instead of in-memory ones,
/// which is important because the service uses SQLite's backup API to fork databases.
/// </summary>
public class SnapshotAtCommitServiceFileBasedTests : IAsyncLifetime
{
    private readonly string _dbPath = $"snapshot-test-{Guid.NewGuid()}.sqlite";
    private readonly CrdtProject _crdtProject;
    private readonly AsyncServiceScope _services;
    private LcmCrdtDbContext _dbContext = null!;
    private IMiniLcmApi _api = null!;
    private SnapshotAtCommitService _service = null!;
    private DataModel _dataModel = null!;

    private static readonly AutoFaker AutoFaker = new(AutoFakerDefault.MakeConfig(["en"]));

    public SnapshotAtCommitServiceFileBasedTests()
    {
        _crdtProject = new CrdtProject("file-based-test", _dbPath);
        var services = new ServiceCollection()
            .AddTestLcmCrdtClient(_crdtProject)
            .BuildServiceProvider();
        _services = services.CreateAsyncScope();
    }

    public async Task InitializeAsync()
    {
        var projectData = new ProjectData("File Based Test", "file-based-test", Guid.NewGuid(), null, Guid.NewGuid());
        var currentProjectService = _services.ServiceProvider.GetRequiredService<CurrentProjectService>();

        // Set up project context for new DB (doesn't query DB)
        _crdtProject.Data = projectData;
        currentProjectService.SetupProjectContextForNewDb(_crdtProject);

        // Now create DbContext - it will use the project context
        _dbContext = await _services.ServiceProvider
            .GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>()
            .CreateDbContextAsync();
        await _dbContext.Database.OpenConnectionAsync();

        // Initialize project in DB
        await CrdtProjectsService.InitProjectDb(_dbContext, projectData);

        // Refresh project data from DB
        await currentProjectService.RefreshProjectData();

        _api = _services.ServiceProvider.GetRequiredService<IMiniLcmApi>();
        _service = _services.ServiceProvider.GetRequiredService<SnapshotAtCommitService>();
        _dataModel = _services.ServiceProvider.GetRequiredService<DataModel>();

        // Seed writing systems for search functionality
        await _api.CreateWritingSystem(new WritingSystem()
        {
            Id = Guid.NewGuid(),
            WsId = "en",
            Name = "English",
            Abbreviation = "en",
            Font = "Arial",
            Exemplars = ["a", "b"],
            Type = WritingSystemType.Vernacular
        });
        await _api.CreateWritingSystem(new WritingSystem()
        {
            Id = Guid.NewGuid(),
            WsId = "en",
            Name = "English",
            Abbreviation = "en",
            Font = "Arial",
            Type = WritingSystemType.Analysis
        });
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.CloseConnectionAsync();
        SqliteConnection.ClearAllPools();
        await _dbContext.Database.EnsureDeletedAsync();
        await _services.DisposeAsync();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task FileBasedDatabase_CanForkAndSnapshot()
    {
        // Arrange: Create entries
        var entry1 = await AutoFaker.EntryReadyForCreation(_api);
        await _api.CreateEntry(entry1);

        var dbContext = _dbContext as ICrdtDbContext;
        var commit1 = await dbContext.Commits.OrderByDescending(c => c.HybridDateTime.DateTime).FirstAsync();
        var expectedSnapshot = await _api.TakeProjectSnapshot();

        // Act: Create second entry
        var entry2 = await AutoFaker.EntryReadyForCreation(_api);
        await _api.CreateEntry(entry2);

        // Get snapshot at first commit using file-based fork
        var actualSnapshot = await _service.GetProjectSnapshotAtCommit(commit1.Id);

        // Verify: Snapshot should match expected state
        actualSnapshot.Should().NotBeNull();
        actualSnapshot.Entries.Should().HaveCount(expectedSnapshot.Entries.Length);
        actualSnapshot.Entries.Should().Contain(e => e.Id == entry1.Id);
        actualSnapshot.Entries.Should().NotContain(e => e.Id == entry2.Id);

        // Verify original DB is unchanged
        await _dataModel.RegenerateSnapshots();
        var currentSnapshot = await _api.TakeProjectSnapshot();
        currentSnapshot.Entries.Should().Contain(e => e.Id == entry1.Id);
        currentSnapshot.Entries.Should().Contain(e => e.Id == entry2.Id);

        // Verify the original database file still exists and is valid
        File.Exists(_dbPath).Should().BeTrue();
        var fileInfo = new FileInfo(_dbPath);
        fileInfo.Length.Should().BeGreaterThan(0);
    }
}
