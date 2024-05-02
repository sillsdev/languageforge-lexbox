using System.Text.Json;
using Crdt.Core;
using LexData;
using LexData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testing.Fixtures;

namespace Testing.LexCore;

[Collection(nameof(TestingServicesFixture))]
public class CrdtCommitTests
{
    private readonly LexBoxDbContext _dbContext;

    public CrdtCommitTests(TestingServicesFixture testing)
    {
        var serviceProvider = testing.ConfigureServices();
        _dbContext = serviceProvider.GetRequiredService<LexBoxDbContext>();
    }

    [Fact]
    public async Task CanSaveCrdtCommit()
    {
        var projectId = await _dbContext.Projects.Select(p => p.Id).FirstAsync();
        var commitId = Guid.NewGuid();
        _dbContext.Add(new CrdtCommit(commitId)
        {
            ClientId = Guid.NewGuid(),
            HybridDateTime = HybridDateTime.ForTestingNow,
            ProjectId = projectId,
            ChangeEntities =
            [
                new ChangeEntity<JsonChange>()
                {
                    Index = 0,
                    CommitId = commitId,
                    EntityId = Guid.NewGuid(),
                    Change = JsonDocument.Parse("""{"$type": "test"}""").RootElement
                }
            ]
        });
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CanRoundTripCommitChanges()
    {

        var projectId = await _dbContext.Projects.Select(p => p.Id).FirstAsync();
        var commitId = Guid.NewGuid();
        var changeJson = """{"$type":"test","name":"Joe"}""";
        var expectedCommit = new CrdtCommit(commitId)
        {
            ClientId = Guid.NewGuid(),
            HybridDateTime = HybridDateTime.ForTestingNow,
            ProjectId = projectId,
            ChangeEntities =
            [
                new ChangeEntity<JsonChange>()
                {
                    Index = 0,
                    CommitId = commitId,
                    EntityId = Guid.NewGuid(),
                    Change = JsonDocument.Parse(changeJson).RootElement
                }
            ]
        };
        _dbContext.Add(expectedCommit);
        await _dbContext.SaveChangesAsync();

        var actualCommit = await _dbContext.Set<CrdtCommit>().AsNoTracking().FirstAsync(c => c.Id == commitId);
        actualCommit.ShouldNotBeSameAs(expectedCommit);
        JsonSerializer.Serialize(actualCommit.ChangeEntities[0].Change).ShouldBe(changeJson);
    }

    [Fact]
    public void TypePropertyShouldAlwaysBeFirst()
    {
        var changeJson = """{"name":"Joe","$type":"test"}""";
        var jsonChange = JsonSerializer.Deserialize<JsonChange>(changeJson);
        JsonSerializer.Serialize(jsonChange).ShouldBe("""{"$type":"test","name":"Joe"}""");
    }
}
