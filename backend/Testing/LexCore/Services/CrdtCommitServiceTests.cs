using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Equivalency;
using LexBoxApi.Services;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIL.Harmony.Core;
using Testing.Fixtures;

namespace Testing.LexCore.Services;

[Collection(nameof(TestingServicesFixture))]
[Trait("Category", "RequiresDb")]
public class CrdtCommitServiceTests
{
    private readonly CrdtCommitService _crdtCommitService;
    private readonly LexBoxDbContext _lexBoxDbContext;

    public CrdtCommitServiceTests(TestingServicesFixture testing)
    {
        var serviceProvider = testing.ConfigureServices(collection => collection.AddSingleton<CrdtCommitService>());
        _crdtCommitService = serviceProvider.GetRequiredService<CrdtCommitService>();
        _lexBoxDbContext = serviceProvider.GetRequiredService<LexBoxDbContext>();
    }

    private ServerCommit CreateCommit(Guid clientId, Guid entityId, DateTime dateTime, Guid? commitId = null)
    {
        commitId ??= Guid.NewGuid();
        var commit = new ServerCommit(commitId.Value)
        {
            ClientId = clientId,
            HybridDateTime = new HybridDateTime(dateTime, 0),
            ProjectId = Guid.Empty,
            ChangeEntities =
            [
                new ChangeEntity<ServerJsonChange>()
                {
                    Index = 0,
                    CommitId = commitId.Value,
                    EntityId = entityId,
                    Change = new()
                    {
                        Type = "MyTestType",
                        ExtensionData = new Dictionary<string, JsonElement>()
                        {
                            ["MyTestProperty"] = JsonSerializer.SerializeToElement("MyTestValue")
                        }
                    }
                }
            ]
        };
        return commit;
    }

    private async IAsyncEnumerable<ServerCommit> AsAsync(IEnumerable<ServerCommit> commits)
    {
        foreach (var serverCommit in commits)
        {
            yield return serverCommit;
        }
    }


    [Fact]
    public async Task CanAddCommits()
    {
        var projectId = await _lexBoxDbContext.Projects.Select(p => p.Id).FirstOrDefaultAsync();
        var commit = CreateCommit(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        await _crdtCommitService.AddCommits(projectId, AsAsync([commit]));
        commit.ProjectId = projectId;
        var actualCommit = _lexBoxDbContext.CrdtCommits(commit.ProjectId).Where(c => c.Id == commit.Id).Should().ContainSingle().Subject;
        actualCommit.Should().BeEquivalentTo(commit,
            options => options
                .Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(10)))
                .WhenTypeIs<DateTimeOffset>()
                .Using<JsonElement>(ctx => ctx.Subject.ToString().Should().Be(ctx.Expectation.ToString()))
                .WhenTypeIs<JsonElement>());
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task CanAddManyCommits(int count)
    {
        var projectId = await _lexBoxDbContext.Projects.Select(p => p.Id).FirstOrDefaultAsync();
        var clientId = Guid.NewGuid();
        var commits = Enumerable.Range(0, count).Select(i => CreateCommit(clientId, Guid.NewGuid(), DateTime.UtcNow.AddSeconds(i))).ToArray();
        await _crdtCommitService.AddCommits(projectId, AsAsync(commits));
        _lexBoxDbContext.CrdtCommits(projectId).Where(c => c.ClientId == clientId).Should()
            .BeEquivalentTo(commits,
                o => o.Using<DateTimeOffset>(ctx =>
                        ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(10)))
                    .WhenTypeIs<DateTimeOffset>()
                    .Using<JsonElement>(ctx => ctx.Subject.ToString().Should().Be(ctx.Expectation.ToString()))
                    .WhenTypeIs<JsonElement>()
                    .Excluding(c => c.ProjectId));
    }

    [Fact]
    public async Task AddingViaServiceBulkAddWorksTheSameAsAddingViaDbContext()
    {
        var projectId = await _lexBoxDbContext.Projects.Select(p => p.Id).FirstOrDefaultAsync();
        var clientId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var dateTime = DateTime.UtcNow;
        var commit = CreateCommit(clientId, entityId, dateTime);
        commit.ProjectId = projectId;
        _lexBoxDbContext.Add(commit);
        await _lexBoxDbContext.SaveChangesAsync();

        var commit2 = CreateCommit(clientId, entityId, dateTime);
        await _crdtCommitService.AddCommits(projectId, AsAsync([commit2]));
        commit2.ProjectId = projectId;
        var commits = await _lexBoxDbContext.Set<ServerCommit>().Where(c => c.ClientId == clientId).ToListAsync();
        commits.Count.Should().Be(2);

        commits[0].Should().BeEquivalentTo(commit, Config);
        commits[1].Should().BeEquivalentTo(commit2, Config);
        commits[0].Should().BeEquivalentTo(commits[1], Config);

        EquivalencyOptions<ServerCommit> Config(EquivalencyOptions<ServerCommit> options)
        {
            return options.Excluding(c => c.Id)
                .Excluding(c => c.CompareKey)
                .For(c => c.ChangeEntities).Exclude(c => c.CommitId)
                .Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(10)))
                .WhenTypeIs<DateTimeOffset>()
                .Using<JsonElement>(ctx => ctx.Subject.ToString().Should().Be(ctx.Expectation.ToString()))
                .WhenTypeIs<JsonElement>();
        }
    }

    [Fact]
    public async Task AddingTheSameCommitTwiceShouldNotThrow()
    {
        var commit = await AddTestCommit();
        var act = async () => await _crdtCommitService.AddCommits(commit.ProjectId, AsAsync([commit]));
        await act.Should().NotThrowAsync();
        _lexBoxDbContext.CrdtCommits(commit.ProjectId).Should().HaveCountGreaterThan(0);
    }


    private async Task<ServerCommit> AddTestCommit()
    {
        var projectId = await _lexBoxDbContext.Projects.Select(p => p.Id).FirstOrDefaultAsync();
        var commit = CreateCommit(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        await _crdtCommitService.AddCommits(projectId, AsAsync([commit]));
        commit.ProjectId = projectId;
        return commit;
    }

    [Fact]
    public async Task CanGetSyncState()
    {
        var commit = await AddTestCommit();
        var syncState = await _crdtCommitService.GetSyncState(commit.ProjectId);
        syncState.ClientHeads.Should().Contain(commit.ClientId, commit.DateTime.ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task CanGetMissingCommits()
    {
        var commit = await AddTestCommit();
        var syncState = await _crdtCommitService.GetSyncState(commit.ProjectId);

        var commits = await _crdtCommitService.GetMissingCommits(commit.ProjectId, syncState, new SyncState([])).ToArrayAsync();
        commits.Should().Contain(c => c.Id == commit.Id);
    }
}
