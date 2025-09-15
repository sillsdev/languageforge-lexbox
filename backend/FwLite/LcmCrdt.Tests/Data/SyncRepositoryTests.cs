using LcmCrdt.Data;

namespace LcmCrdt.Tests.Data;

public class SyncRepositoryTests: IAsyncLifetime
{
    private readonly MiniLcmApiFixture _apiFixture;
    private SyncRepository _syncRepository = null!;

    public SyncRepositoryTests()
    {
        _apiFixture = MiniLcmApiFixture.Create(false);
    }

    public async Task InitializeAsync()
    {
        await _apiFixture.InitializeAsync();
        _syncRepository = _apiFixture.GetService<SyncRepository>();
    }

    public async Task DisposeAsync()
    {
        await _apiFixture.DisposeAsync();
    }

    [Fact]
    public async Task GetLatestSyncedCommitDate_WhenNoCommits_ReturnsNull()
    {
        var latestCommitDate = await _syncRepository.GetLatestSyncedCommitDate();
        latestCommitDate.Should().BeNull();
    }
}
