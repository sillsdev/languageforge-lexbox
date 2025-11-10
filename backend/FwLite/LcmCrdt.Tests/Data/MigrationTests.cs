using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
    private static readonly JsonSerializerOptions IndentedHarmonyJsonOptions = new(TestJsonOptions.Harmony())
    {
        WriteIndented = true
    };

    [ModuleInitializer]
    internal static void Init()
    {
        VerifySystemJson.Initialize();
    }

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

        var snapshotsJson = JsonSerializer.Serialize(snapshots, IndentedHarmonyJsonOptions);
        var changesJson = JsonSerializer.Serialize(changes, IndentedHarmonyJsonOptions);
        var projectJson = JsonSerializer.Serialize(project, IndentedHarmonyJsonOptions);

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


    [Theory]
    [InlineData(RegressionTestHelper.RegressionVersion.v1)]
    [InlineData(RegressionTestHelper.RegressionVersion.v2)]
    public async Task VerifyRegeneratedSnapshotsAfterMigrationFromScriptedDb(RegressionTestHelper.RegressionVersion regressionVersion)
    {
        await _helper.InitializeAsync(regressionVersion);
        var api = _helper.Services.GetRequiredService<IMiniLcmApi>();
        var crdtConfig = _helper.Services.GetRequiredService<IOptions<CrdtConfig>>().Value;

        await using var dbContext = await _helper.Services.GetRequiredService<ICrdtDbContextFactory>().CreateDbContextAsync();
        await using var dataModel = _helper.Services.GetRequiredService<DataModel>();
        var syncable = dataModel as ISyncable;

        var noOpCommit = FakeCommit.NoOp(new HybridDateTime(DateTimeOffset.MinValue, 0));
        await syncable.AddRangeFromSync([noOpCommit]);

        var commits = await dbContext.Commits.AsNoTracking().ToArrayAsync();
        var commitIdToHybridDateTime = commits.ToDictionary(c => c.Id, c => c.HybridDateTime);

        var latestSnapshots = (await dataModel.GetLatestSnapshots()
            .ToArrayAsync())
            .OrderBy(s => s.TypeName)
            .ThenBy(s => s.EntityId)
            .ThenBy(c => commitIdToHybridDateTime[c.CommitId].DateTime)
            .ThenBy(c => commitIdToHybridDateTime[c.CommitId].Counter)
            .ThenBy(c => c.CommitId);

        // this happens to result in null properties being omitted, which is probably fine
        // strangely VerifyJson(string).UseStrictJson() keeps null properties, but omits empty arrays and objects, which seems bizarre
        var snapshotsJson = JsonSerializer.SerializeToDocument(latestSnapshots, IndentedHarmonyJsonOptions);

        // it would be nice to only scrub the snapshot Guid, because those are the only ones that should be different,
        // but Verify doesn't have a way to do that, so we just let it scrub all of them
        await Verify(snapshotsJson)
            .UseStrictJson()
            .UseFileName($"VerifyRegeneratedSnapshotsAfterMigrationFromScriptedDb.{regressionVersion}");
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
        public static Commit NoOp(HybridDateTime hybridDateTime)
        {
            return new FakeCommit(Guid.NewGuid(), hybridDateTime)
            {
                ClientId = Guid.NewGuid(),
                ChangeEntities = [],
            };
        }

        [SetsRequiredMembers]
        public FakeCommit(Guid id, HybridDateTime hybridDateTime) : base(id, "", NullParentHash, hybridDateTime)
        {
            HybridDateTime = hybridDateTime;
            SetParentHash(NullParentHash);
        }
    }
}
