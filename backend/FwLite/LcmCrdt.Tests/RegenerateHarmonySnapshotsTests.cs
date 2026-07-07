using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniLcm.Tests.AutoFakerHelpers;
using SIL.Harmony.Db;
using Soenneker.Utils.AutoBogus;

namespace LcmCrdt.Tests;

public class RegenerateHarmonySnapshotsTests
{
    private static readonly AutoFaker AutoFaker = new(AutoFakerDefault.MakeConfig(["en"]));

    [Fact]
    public async Task RegenerateHarmonySnapshotsAsync_StateUnchanged()
    {
        var code = $"regen-test-{Guid.NewGuid():N}";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        builder.Services.Configure<LcmCrdtConfig>(config => config.EnableProjectDataFileCache = false);
        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();
        var projectsService = scope.ServiceProvider.GetRequiredService<CrdtProjectsService>();
        var project = await projectsService.CreateExampleProject(code);
        var api = (CrdtMiniLcmApi)await scope.ServiceProvider.OpenCrdtProject(project);
        var dataModel = scope.ServiceProvider.GetRequiredService<DataModel>();

        var entry = await AutoFaker.EntryReadyForCreation(api);
        await api.CreateEntry(entry);

        var beforeSnapshot = await dataModel.GetLatestSnapshotByObjectId(entry.Id);
        var beforeEntry = (Entry)beforeSnapshot.Entity.DbObject;
        var beforeProjectSnapshot = await api.TakeProjectSnapshot();

        await using var regenScope = host.Services.CreateAsyncScope();
        await regenScope.ServiceProvider.GetRequiredService<CrdtProjectsService>()
            .RegenerateHarmonySnapshotsAsync(code);

        await using var dbContext = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>()
            .CreateDbContextAsync();
        ICrdtDbContext crdtDbContext = dbContext;

        (await crdtDbContext.Snapshots.AnyAsync(s => s.Id == beforeSnapshot.Id)).Should().BeFalse();

        var afterSnapshot = await dataModel.GetLatestSnapshotByObjectId(entry.Id);
        afterSnapshot.Id.Should().NotBe(beforeSnapshot.Id);
        afterSnapshot.CommitId.Should().Be(beforeSnapshot.CommitId);

        var afterEntry = (Entry)afterSnapshot.Entity.DbObject;
        afterEntry.Should().BeEquivalentTo(beforeEntry);

        var dbEntry = await dbContext.Entries.FirstAsync(e => e.Id == entry.Id);
        dbEntry.Should().BeEquivalentTo(beforeEntry);

        var afterProjectSnapshot = await api.TakeProjectSnapshot();
        afterProjectSnapshot.Should().BeEquivalentTo(beforeProjectSnapshot, options => options
            .WithStrictOrdering()
            .WithoutStrictOrderingFor(x => x.PartsOfSpeech)
            .WithoutStrictOrderingFor(x => x.Publications)
            .WithoutStrictOrderingFor(x => x.SemanticDomains)
            .WithoutStrictOrderingFor(x => x.ComplexFormTypes)
            .WithoutStrictOrderingFor(x => x.MorphTypes));

        await dbContext.Database.EnsureDeletedAsync();
    }
}
