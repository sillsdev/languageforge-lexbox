using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SIL.Harmony.Core;
using SIL.Harmony.Db;

namespace LcmCrdt.Tests.Data;

public class MigrationTests : IAsyncLifetime
{
    private readonly RegressionTestHelper _helper = new("MigrationTest");

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _helper.DisposeAsync();
    }

    [Theory]
    [InlineData(RegressionTestHelper.RegressionVersion.v1)]
    [InlineData(RegressionTestHelper.RegressionVersion.v2)]
    public async Task GetEntries_WorksAfterMigrationFromScriptedDb(RegressionTestHelper.RegressionVersion regressionVersion)
    {
        await _helper.InitializeAsync(regressionVersion);
        var api = _helper.Services.GetRequiredService<IMiniLcmApi>();
        var hasEntries = false;
        await foreach (var entry in api.GetEntries(new(Count: 100)))
        {
            hasEntries = true;
            entry.Should().NotBeNull();
        }

        hasEntries.Should().BeTrue();
    }

    [Theory]
    [InlineData(RegressionTestHelper.RegressionVersion.v1)]
    [InlineData(RegressionTestHelper.RegressionVersion.v2)]
    public async Task VerifyAfterMigrationFromScriptedDb(RegressionTestHelper.RegressionVersion regressionVersion)
    {
        await _helper.InitializeAsync(regressionVersion);
        var api = _helper.Services.GetRequiredService<IMiniLcmApi>();
        var crdtConfig = _helper.Services.GetRequiredService<IOptions<CrdtConfig>>().Value;
        // todo use TestJsonOptions instead
        var jsonSerializerOptions = new JsonSerializerOptions(crdtConfig.JsonSerializerOptions)
        {
            WriteIndented = true
        };

        await using var dbContext = await _helper.Services.GetRequiredService<ICrdtDbContextFactory>().CreateDbContextAsync();
        var snapshots = await dbContext.Snapshots.AsNoTracking()
            .OrderBy(s => s.TypeName)
            .ThenBy(s => s.EntityId)
            .ThenBy(c => c.Commit.HybridDateTime.DateTime)
            .ThenBy(c => c.Commit.HybridDateTime.Counter)
            .ThenBy(c => c.Commit.Id)
            .ToArrayAsync();

        var entityIdToTypeName = snapshots
            .GroupBy(s => s.EntityId)
            .ToDictionary(g => g.Key, g => g.First().TypeName);
        var changes = (await dbContext.Commits.AsNoTracking().Include(c => c.ChangeEntities)
            .ToArrayAsync())
            .SelectMany(c => c.ChangeEntities.Select(changeEntity => new { ChangeEntity = changeEntity, c.HybridDateTime }))
            .OrderBy(s => entityIdToTypeName[s.ChangeEntity.EntityId])
            .ThenBy(c => c.ChangeEntity.EntityId)
            .ThenBy(c => c.HybridDateTime.DateTime)
            .ThenBy(c => c.HybridDateTime.Counter)
            .ThenBy(c => c.ChangeEntity.CommitId)
            .Select(c => c.ChangeEntity)
            .ToArray();
        var project = await TakeProjectSnapshot(api);

        var snapshotsJson = JsonSerializer.Serialize(snapshots, jsonSerializerOptions);
        var changesJson = JsonSerializer.Serialize(changes, jsonSerializerOptions);
        var projectJson = JsonSerializer.Serialize(project, jsonSerializerOptions);

        await Task.WhenAll(
            Verify(snapshotsJson)
                .UseStrictJson()
                .UseFileName($"MigrationTests_FromScriptedDb.{regressionVersion}.Snapshots"),
            Verify(changesJson)
                .UseStrictJson()
                .UseFileName($"MigrationTests_FromScriptedDb.{regressionVersion}.ChangeEntities"),
            Verify(projectJson)
                .UseStrictJson()
                .UseFileName($"MigrationTests_FromScriptedDb.{regressionVersion}.ProjectSnapshot")
        );
    }

    private static async Task<ProjectSnapshot> TakeProjectSnapshot(IMiniLcmReadApi api)
    {
        return new ProjectSnapshot(
            await api.GetAllEntries()
                .OrderBy(e => e.Id)
                .ToArrayAsync(),
            await api.GetPartsOfSpeech()
                .OrderBy(p => p.Id)
                .ToArrayAsync(),
            await api.GetPublications()
                .OrderBy(p => p.Id)
                .ToArrayAsync(),
            await api.GetSemanticDomains()
                .OrderBy(d => d.Id)
                .ToArrayAsync(),
            await api.GetComplexFormTypes()
                .OrderBy(c => c.Id)
                .ToArrayAsync(),
            await api.GetWritingSystems());
    }

    private class FakeCommit : Commit
    {
        [SetsRequiredMembers]
        public FakeCommit(Guid id, HybridDateTime hybridDateTime) : base(id, "", NullParentHash, hybridDateTime)
        {
            HybridDateTime = hybridDateTime;
            SetParentHash(NullParentHash);
        }
    }
}
