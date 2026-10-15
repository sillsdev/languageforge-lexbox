using LcmCrdt.Changes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LcmCrdt.Tests;

public class MorphTypeSeedingTests
{
    [Fact]
    public async Task BlankNewProject_HasNoMorphTypesInDb()
    {
        var code = "morph-type-blank-test";
        var sqliteFile = $"{code}.sqlite";
        if (File.Exists(sqliteFile)) File.Delete(sqliteFile);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();

        var crdtProjectsService = scope.ServiceProvider.GetRequiredService<CrdtProjectsService>();
        var crdtProject = await crdtProjectsService.CreateProject(new(
            Name: "MorphTypeBlankTest",
            Code: code,
            Path: ""));

        var api = (CrdtMiniLcmApi)await scope.ServiceProvider.OpenCrdtProject(crdtProject);
        var morphTypes = await api.GetMorphTypes().ToArrayAsync();

        morphTypes.Should().BeEmpty(
            "blank CreateProject no longer seeds morph types on migrate (#2350)");

        await using var dbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        await dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task TemplatedProject_HasCanonicalMorphTypesWithoutRedundantMigrateSeed()
    {
        var code = $"morph-type-seed-templated-{Guid.NewGuid():N}";
        if (File.Exists($"{code}.sqlite")) File.Delete($"{code}.sqlite");
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();

        var crdtProjectsService = scope.ServiceProvider.GetRequiredService<CrdtProjectsService>();
        var crdtProject = await crdtProjectsService.CreateProjectFromTemplate(new(
            Name: "MorphTypeSeedTemplated",
            Code: code,
            Path: "",
            Role: UserProjectRole.Manager),
            vernacularWs: "fr");

        var api = (CrdtMiniLcmApi)await scope.ServiceProvider.OpenCrdtProject(crdtProject);
        var morphTypes = await api.GetMorphTypes().ToArrayAsync();
        morphTypes.Should().HaveCount(CanonicalMorphTypes.All.Count);

        await using var dbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();

        var morphTypeCreatingChanges = await dbContext.Database.SqlQuery<int>(
            $"""
             SELECT COUNT(*) AS Value FROM ChangeEntities
             WHERE json_extract(Change, '$."$type"') = {nameof(CreateMorphTypeChange)}
             """).SingleAsync();
        morphTypeCreatingChanges.Should().Be(CanonicalMorphTypes.All.Count,
            "the template import creates exactly the canonical morph types; MigrateDb must not add a redundant seed on open");

        await dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public void CanonicalMorphTypes_CoverAllKindsExceptUnknown()
    {
        var allKinds = Enum.GetValues<MorphTypeKind>()
            .Where(k => k != MorphTypeKind.Unknown)
            .ToHashSet();

        CanonicalMorphTypes.All.Keys.Should().BeEquivalentTo(allKinds);
    }

    [Fact]
    public void CanonicalMorphTypes_HaveRequiredFields()
    {
        foreach (var mt in CanonicalMorphTypes.All.Values)
        {
            mt.Id.Should().NotBe(Guid.Empty, $"MorphType {mt.Kind} should have a non-empty Id");
            mt.Name["en"].Should().NotBeNullOrWhiteSpace($"MorphType {mt.Kind} should have an English name");
            mt.Abbreviation["en"].Should().NotBeNullOrWhiteSpace($"MorphType {mt.Kind} should have an English abbreviation");
            mt.Description["en"].IsEmpty.Should().BeFalse($"MorphType {mt.Kind} should have an English description");
        }
    }
}
