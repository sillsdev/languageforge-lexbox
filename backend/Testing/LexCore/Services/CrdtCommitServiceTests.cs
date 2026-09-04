using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Equivalency;
using LexBoxApi.Services;
using LexData;
using LinqToDB.EntityFrameworkCore;
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

    private IAsyncEnumerable<ServerCommit> AsAsync(IEnumerable<ServerCommit> commits)
    {
        return commits.ToAsyncEnumerable();
    }

    //previously the value of the Change property was serialized twice, this test ensures that we can still
    //pull those commits out of the database
    [Fact]
    public async Task CanQueryOldCommits()
    {
        var projectId = await _lexBoxDbContext.Projects.Select(p => p.Id).FirstOrDefaultAsync();
        var context = _lexBoxDbContext.CreateLinqToDBContext();
        var commitId = Guid.NewGuid();
        var changeEntity = new ChangeEntity<ServerJsonChange>
        {
            Index = 0,
            CommitId = commitId,
            EntityId = Guid.NewGuid(),
            Change = new()
            {
                Type = "MyTestType",
                ExtensionData = new Dictionary<string, JsonElement>()
                {
                    ["MyTestProperty"] = JsonSerializer.SerializeToElement("MyTestValue")
                }
            }
        };
        var changeEntityJson = JsonSerializer.SerializeToNode(changeEntity);
        changeEntityJson.Should().NotBeNull();
        //the old format stored json in json, this is emulating that.
        changeEntityJson["Change"] = changeEntityJson["Change"]?.ToJsonString();
        var jsonPayload = changeEntityJson.ToJsonString();
        //Insert a synthetic old-format commit via raw SQL so we can put pre-serialized
        //JSON in ChangeEntities. Linq2Db v6 unconditionally wraps any column assignment
        //(including Sql.Expr) in the EF JSON value converter inside an InsertAsync
        //projection lambda, so we can't use the typed API for this test case.
        var inlinePayload = $"[{jsonPayload}]";
        await LinqToDB.Data.DataContextExtensions.ExecuteAsync(
            context,
            """
            INSERT INTO "CrdtCommits"
                ("Id", "ClientId", "HybridDateTime_DateTime", "HybridDateTime_Counter", "ProjectId", "Metadata", "ChangeEntities")
            VALUES (@id, @clientId, @dt, 0, @projectId, '{}'::jsonb, @payload::jsonb)
            """,
            new LinqToDB.Data.DataParameter("id", commitId, LinqToDB.DataType.Guid),
            new LinqToDB.Data.DataParameter("clientId", Guid.NewGuid(), LinqToDB.DataType.Guid),
            new LinqToDB.Data.DataParameter("dt", DateTimeOffset.UtcNow, LinqToDB.DataType.DateTimeOffset),
            new LinqToDB.Data.DataParameter("projectId", projectId, LinqToDB.DataType.Guid),
            new LinqToDB.Data.DataParameter("payload", inlinePayload, LinqToDB.DataType.NVarChar));
        var commits = await _lexBoxDbContext.CrdtCommits(projectId).ToArrayAsync();
        var actualCommit = commits.Should().ContainSingle(c => c.Id == commitId).Subject;
        actualCommit.ChangeEntities.Should().BeEquivalentTo([changeEntity],
            options => options
                .Using<JsonElement>(ctx => ctx.Subject.ToString().Should().Be(ctx.Expectation.ToString()))
                .WhenTypeIs<JsonElement>()
            );
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

    [Fact]
    public async Task SnapshotRebuildCommitPredatesEveryOtherCommitAndCarriesNoChanges()
    {
        var existing = await AddTestCommit();

        var rebuild = await _crdtCommitService.AddSnapshotRebuildCommit(existing.ProjectId);

        rebuild.Should().NotBeNull();
        var commits = await _lexBoxDbContext.CrdtCommits(existing.ProjectId).ToArrayAsync();
        var added = commits.Should().ContainSingle(c => c.Id == rebuild!.CommitId).Subject;
        //an empty commit can't change any data, it only forces the replay
        added.ChangeEntities.Should().BeEmpty();
        added.DateTime.Should().BeBefore(commits.Where(c => c.Id != added.Id).Min(c => c.DateTime));
        rebuild!.CommitsToReplay.Should().Be(commits.Length - 1);
    }

    //the point of the rebuild commit: clients with nothing left to sync must still be sent it
    [Fact]
    public async Task SnapshotRebuildCommitIsSentToAClientThatIsAlreadyUpToDate()
    {
        var existing = await AddTestCommit();
        var upToDateClient = await _crdtCommitService.GetSyncState(existing.ProjectId);

        var rebuild = await _crdtCommitService.AddSnapshotRebuildCommit(existing.ProjectId);

        var serverState = await _crdtCommitService.GetSyncState(existing.ProjectId);
        var missing = await _crdtCommitService.GetMissingCommits(existing.ProjectId, serverState, upToDateClient)
            .ToArrayAsync();
        missing.Should().ContainSingle().Which.Id.Should().Be(rebuild!.CommitId);
    }

    [Fact]
    public async Task NoSnapshotRebuildCommitIsAddedForAProjectWithNoCommits()
    {
        var rebuild = await _crdtCommitService.AddSnapshotRebuildCommit(Guid.NewGuid());
        rebuild.Should().BeNull();
    }
}
