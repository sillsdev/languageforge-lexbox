using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIL.Harmony.Core;

namespace LcmCrdt.Tests.Data;

public class DownloadProjectTests : IAsyncLifetime
{
    private readonly RegressionTestHelper _helper = new("DownloadProject");
    private static readonly Guid _projectId = new("B467051E-A492-4E5B-9C17-858D7797292C");//internal project Id of v2 project
    private readonly MiniLcmApiFixture _apiFixture = MiniLcmApiFixture.Create(false, _projectId);

    public async Task InitializeAsync()
    {
        await _helper.InitializeAsync(RegressionTestHelper.RegressionVersion.v2);
        //add a change after migration which creates MorphTypes
        await _helper.Services.GetRequiredService<IMiniLcmApi>().CreateEntry(new Entry()
        {
            LexemeForm = {{"en", "test"}}
        });
        await _apiFixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _helper.DisposeAsync();
        await _apiFixture.DisposeAsync();
    }

    [Fact]
    public async Task CanCreateANewProjectViaSync()
    {
        var remoteModel = _helper.Services.GetRequiredService<DataModel>();
        var remoteDb = await _helper.Services.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();

        await _apiFixture.DataModel.SyncWith(remoteModel);
        var localCommits = await _apiFixture.DbContext.Set<Commit>().DefaultOrder().ToListAsync();
        var remoteCommits = await remoteDb.Set<Commit>().DefaultOrder().ToListAsync();
        localCommits.Count.Should().Be(remoteCommits.Count);
        for (var i = localCommits.Count - 1; i >= 0; i--)
        {
            var localCommit = localCommits[i];
            var remoteCommit = remoteCommits[i];
            localCommit.Should().BeEquivalentTo(remoteCommit, "commit index {0} should be the same", i);
        }
    }
}
