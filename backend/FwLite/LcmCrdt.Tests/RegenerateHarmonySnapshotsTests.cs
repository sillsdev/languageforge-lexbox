using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniLcm.Tests.AutoFakerHelpers;
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

        var entry = await AutoFaker.EntryReadyForCreation(api);
        await api.CreateEntry(entry);

        var before = await api.TakeProjectSnapshot();

        await using var regenScope = host.Services.CreateAsyncScope();
        await regenScope.ServiceProvider.GetRequiredService<CrdtProjectsService>()
            .RegenerateHarmonySnapshotsAsync(code);

        var after = await api.TakeProjectSnapshot();
        after.Should().BeEquivalentTo(before, options => options
            .WithStrictOrdering()
            .WithoutStrictOrderingFor(x => x.PartsOfSpeech)
            .WithoutStrictOrderingFor(x => x.Publications)
            .WithoutStrictOrderingFor(x => x.SemanticDomains)
            .WithoutStrictOrderingFor(x => x.ComplexFormTypes)
            .WithoutStrictOrderingFor(x => x.MorphTypes));

        await using var dbContext = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>()
            .CreateDbContextAsync();
        await dbContext.Database.EnsureDeletedAsync();
    }
}
