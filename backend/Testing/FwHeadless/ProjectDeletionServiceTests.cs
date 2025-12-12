using FwHeadless;
using FwHeadless.Services;
using LexCore.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Testing.FwHeadless;

/// <summary>
/// Unit tests for ProjectDeletionService which handles deletion of FwHeadless project data.
/// </summary>
public class ProjectDeletionServiceTests : IDisposable
{
    private readonly FwHeadlessConfig _config;
    private readonly string _projectCode;
    private readonly Guid _projectId;
    private readonly string _projectFolder;
    private readonly string _crdtFile;
    private readonly string _fwDataFolder;
    private readonly ProjectDeletionService _deletionService;
    private readonly TestProjectLookupService _projectLookupService;
    private readonly TestSyncHostedService _syncHostedService;

    public ProjectDeletionServiceTests()
    {
        _projectCode = "test-" + Guid.NewGuid().ToString("N")[..8];
        _projectId = Guid.NewGuid();

        var tempRoot = Path.Combine(Path.GetTempPath(), $"ProjectDeletionTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        _config = new FwHeadlessConfig
        {
            ProjectStorageRoot = tempRoot,
            LexboxUrl = "http://localhost/",
            LexboxUsername = "test",
            LexboxPassword = "test",
            MediaFileAuthority = "localhost"
        };

        _projectFolder = _config.GetProjectFolder(_projectCode, _projectId);
        _crdtFile = _config.GetCrdtFile(_projectCode, _projectId);
        _fwDataFolder = _config.GetFwDataProject(_projectCode, _projectId).ProjectFolder;

        _projectLookupService = new TestProjectLookupService();
        _syncHostedService = new TestSyncHostedService();

        _deletionService = new ProjectDeletionService(
            Options.Create(_config),
            _projectLookupService,
            _syncHostedService,
            NullLogger<ProjectDeletionService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_config.ProjectStorageRoot))
        {
            Directory.Delete(_config.ProjectStorageRoot, true);
        }
    }

    private void CreateFullProjectStructure()
    {
        Directory.CreateDirectory(_projectFolder);
        Directory.CreateDirectory(_fwDataFolder);
        File.WriteAllText(_crdtFile, "fake crdt content");
        File.WriteAllText(Path.Combine(_fwDataFolder, "test.fwdata"), "fake fwdata");
    }

    [Fact]
    public async Task DeleteRepo_OnlyDeletesFwDataFolder_PreservesCrdtAndProjectFolder()
    {
        // Arrange
        _projectLookupService.RegisterProject(_projectId, _projectCode);
        CreateFullProjectStructure();

        // Act
        var result = await _deletionService.DeleteRepo(_projectId);

        // Assert
        result.Should().BeTrue();
        Directory.Exists(_projectFolder).Should().BeTrue("project folder should still exist");
        File.Exists(_crdtFile).Should().BeTrue("CRDT file should still exist");
        Directory.Exists(_fwDataFolder).Should().BeFalse("FwData folder should be deleted");
    }

    [Fact]
    public async Task DeleteProject_DeletesEverything()
    {
        // Arrange
        _projectLookupService.RegisterProject(_projectId, _projectCode);
        CreateFullProjectStructure();

        // Act
        var result = await _deletionService.DeleteProject(_projectId);

        // Assert
        result.Should().BeTrue();
        Directory.Exists(_projectFolder).Should().BeFalse("entire project folder should be deleted");
    }

    [Fact]
    public async Task DeleteRepo_WhenProjectNotFound_ReturnsFalse()
    {
        var result = await _deletionService.DeleteRepo(_projectId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProject_WhenProjectNotFound_ReturnsFalse()
    {
        var result = await _deletionService.DeleteProject(_projectId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRepo_WhenSyncRunning_ThrowsProjectSyncInProgressException()
    {
        _projectLookupService.RegisterProject(_projectId, _projectCode);
        _syncHostedService.SetJobQueuedOrRunning(_projectId, true);

        await Assert.ThrowsAsync<ProjectSyncInProgressException>(() => _deletionService.DeleteRepo(_projectId));
    }

    [Fact]
    public async Task DeleteProject_WhenSyncRunning_ThrowsProjectSyncInProgressException()
    {
        _projectLookupService.RegisterProject(_projectId, _projectCode);
        _syncHostedService.SetJobQueuedOrRunning(_projectId, true);

        await Assert.ThrowsAsync<ProjectSyncInProgressException>(() => _deletionService.DeleteProject(_projectId));
    }

    [Fact]
    public async Task DeleteRepo_WhenFolderDoesNotExist_StillReturnsTrue()
    {
        _projectLookupService.RegisterProject(_projectId, _projectCode);

        var result = await _deletionService.DeleteRepo(_projectId);

        result.Should().BeTrue();
    }

    // Test helper classes
    private class TestProjectLookupService() : ProjectLookupService(null!)
    {
        private readonly Dictionary<Guid, string> _projects = [];

        public void RegisterProject(Guid id, string code) => _projects[id] = code;

        public override ValueTask<string?> GetProjectCode(Guid projectId) =>
            ValueTask.FromResult(_projects.TryGetValue(projectId, out var code) ? code : null);
    }

    private class TestSyncHostedService(IServiceProvider? services = null, ILogger<SyncHostedService>? logger = null, IMemoryCache? memoryCache = null)
        : SyncHostedService(services ?? null!, logger ?? null!, memoryCache ?? null!)
    {
        private readonly HashSet<Guid> _queuedOrRunning = [];

        public void SetJobQueuedOrRunning(Guid projectId, bool isQueuedOrRunning)
        {
            if (isQueuedOrRunning)
                _queuedOrRunning.Add(projectId);
            else
                _queuedOrRunning.Remove(projectId);
        }

        public override bool IsJobQueuedOrRunning(Guid projectId)
        {
            return _queuedOrRunning.Contains(projectId);
        }
    }
}
