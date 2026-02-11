using FwHeadless;
using FwHeadless.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Testing.FwHeadless;

public class ProjectMetadataServiceTests : IAsyncLifetime
{
    private readonly ILogger<ProjectMetadataService> _logger;
    private FwHeadlessConfig _config = null!;
    private string _tempDir = null!;

    public ProjectMetadataServiceTests()
    {
        _logger = new LoggerFactory().CreateLogger<ProjectMetadataService>();
    }

    public Task InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"metadata-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _config = new FwHeadlessConfig
        {
            LexboxUrl = "http://localhost/",
            LexboxUsername = "test",
            LexboxPassword = "test",
            ProjectStorageRoot = _tempDir,
            MediaFileAuthority = "localhost"
        };
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        return Task.CompletedTask;
    }

    private ProjectMetadataService CreateService()
    {
        var options = Options.Create(_config);
        return new ProjectMetadataService(options, _logger);
    }

    private string CreateTestProjectFolder(Guid projectId, string projectCode = "TEST")
    {
        var folderPath = _config.GetProjectFolder(projectCode, projectId);
        Directory.CreateDirectory(folderPath);
        return folderPath;
    }

    [Fact]
    public async Task GetSyncBlockedInfoAsync_WhenNoBlockSet_ReturnsNull()
    {
        var service = CreateService();
        var projectId = Guid.NewGuid();
        CreateTestProjectFolder(projectId);

        var result = await service.GetSyncBlockedInfoAsync(projectId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task BlockFromSyncAsync_BlocksProject()
    {
        var service = CreateService();
        var projectId = Guid.NewGuid();
        CreateTestProjectFolder(projectId);

        await service.BlockFromSyncAsync(projectId, "Test block");
        var blockInfo = await service.GetSyncBlockedInfoAsync(projectId);

        blockInfo.Should().NotBeNull();
        blockInfo.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task BlockFromSyncAsync_SetsReason()
    {
        var service = CreateService();
        var projectId = Guid.NewGuid();
        CreateTestProjectFolder(projectId);
        const string reason = "Critical issue found";

        await service.BlockFromSyncAsync(projectId, reason);
        var blockInfo = await service.GetSyncBlockedInfoAsync(projectId);

        blockInfo.Should().NotBeNull();
        blockInfo.Reason.Should().Be(reason);
    }

    [Fact]
    public async Task BlockFromSyncAsync_SetsBlockedAtTime()
    {
        var service = CreateService();
        var projectId = Guid.NewGuid();
        CreateTestProjectFolder(projectId);
        var beforeBlock = DateTime.UtcNow;

        await service.BlockFromSyncAsync(projectId, "Test");
        var blockInfo = await service.GetSyncBlockedInfoAsync(projectId);
        var afterBlock = DateTime.UtcNow;

        blockInfo?.BlockedAt.Should().NotBeNull();
        blockInfo!.BlockedAt.Should().BeOnOrAfter(beforeBlock).And.BeOnOrBefore(afterBlock);
    }

    [Fact]
    public async Task UnblockFromSyncAsync_UnblocksProject()
    {
        var service = CreateService();
        var projectId = Guid.NewGuid();
        CreateTestProjectFolder(projectId);

        await service.BlockFromSyncAsync(projectId, "Block");
        await service.UnblockFromSyncAsync(projectId);
        var blockInfo = await service.GetSyncBlockedInfoAsync(projectId);

        blockInfo.Should().NotBeNull();
        blockInfo.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task UnblockFromSyncAsync_ClearsPreviousBlockedInfo()
    {
        var service = CreateService();
        var projectId = Guid.NewGuid();
        CreateTestProjectFolder(projectId);

        await service.BlockFromSyncAsync(projectId, "Block 1");
        await service.UnblockFromSyncAsync(projectId);
        var blockInfo = await service.GetSyncBlockedInfoAsync(projectId);

        blockInfo.Should().NotBeNull();
        blockInfo.IsBlocked.Should().BeFalse();
        blockInfo.Reason.Should().BeNull();
        blockInfo.BlockedAt.Should().BeNull();
    }

    [Fact]
    public async Task MultipleProjects_IndependentBlockStatus()
    {
        var service = CreateService();
        var project1 = Guid.NewGuid();
        var project2 = Guid.NewGuid();
        CreateTestProjectFolder(project1, "P1");
        CreateTestProjectFolder(project2, "P2");

        await service.BlockFromSyncAsync(project1, "Blocked");
        var project1Info = await service.GetSyncBlockedInfoAsync(project1);
        var project2Info = await service.GetSyncBlockedInfoAsync(project2);

        project1Info?.IsBlocked.Should().BeTrue();
        project2Info.Should().BeNull();
    }

    [Fact]
    public async Task PersistsAcrossInstances()
    {
        var projectId = Guid.NewGuid();
        CreateTestProjectFolder(projectId);
        var reason = "Persist test";

        // Block with first instance
        var service1 = CreateService();
        await service1.BlockFromSyncAsync(projectId, reason);

        // Check with second instance
        var service2 = CreateService();
        var blockInfo = await service2.GetSyncBlockedInfoAsync(projectId);

        blockInfo.Should().NotBeNull();
        blockInfo.IsBlocked.Should().BeTrue();
        blockInfo.Reason.Should().Be(reason);
    }

    [Fact]
    public async Task BlockWithoutReason_UsesDefaultReason()
    {
        var service = CreateService();
        var projectId = Guid.NewGuid();
        CreateTestProjectFolder(projectId);

        await service.BlockFromSyncAsync(projectId);
        var blockInfo = await service.GetSyncBlockedInfoAsync(projectId);

        blockInfo.Should().NotBeNull();
        blockInfo.Reason.Should().Be("Manual block");
    }
}
