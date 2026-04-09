using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LcmCrdt.Tests;

public class MorphTypeSeedingTests
{
    [Fact]
    public async Task NewProjectWithSeedData_HasAllCanonicalMorphTypes()
    {
        var code = "morph-type-seed-test";
        var sqliteFile = $"{code}.sqlite";
        if (File.Exists(sqliteFile)) File.Delete(sqliteFile);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();

        var crdtProjectsService = scope.ServiceProvider.GetRequiredService<CrdtProjectsService>();
        var crdtProject = await crdtProjectsService.CreateProject(new(
            Name: "MorphTypeSeedTest",
            Code: code,
            Path: "",
            SeedNewProjectData: true));

        var api = (CrdtMiniLcmApi)await scope.ServiceProvider.OpenCrdtProject(crdtProject);
        var morphTypes = await api.GetMorphTypes().ToArrayAsync();

        morphTypes.Should().BeEquivalentTo(CanonicalMorphTypes.All.Values);

        await using var dbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        await dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task ExistingProjectWithoutMorphTypes_GetsMorphTypesOnOpen()
    {
        var code = "morph-type-seed-existing";
        var sqliteFile = $"{code}.sqlite";
        if (File.Exists(sqliteFile)) File.Delete(sqliteFile);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();

        var crdtProjectsService = scope.ServiceProvider.GetRequiredService<CrdtProjectsService>();
        // Create project WITHOUT seeding
        var crdtProject = await crdtProjectsService.CreateProject(new(
            Name: "MorphTypeSeedExisting",
            Code: code,
            Path: "",
            SeedNewProjectData: false));

        // Opening the project triggers MigrateDb, which seeds morph types if missing
        var api = (CrdtMiniLcmApi)await scope.ServiceProvider.OpenCrdtProject(crdtProject);
        var morphTypes = await api.GetMorphTypes().ToArrayAsync();

        morphTypes.Should().HaveCount(CanonicalMorphTypes.All.Count);

        await using var dbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        await dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task SeedingIsIdempotent_OpeningProjectTwiceDoesNotDuplicate()
    {
        var code = "morph-type-seed-idempotent";
        var sqliteFile = $"{code}.sqlite";
        if (File.Exists(sqliteFile)) File.Delete(sqliteFile);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();

        // First open: seed morph types
        {
            await using var scope = host.Services.CreateAsyncScope();
            var crdtProjectsService = scope.ServiceProvider.GetRequiredService<CrdtProjectsService>();
            var crdtProject = await crdtProjectsService.CreateProject(new(
                Name: "MorphTypeSeedIdempotent",
                Code: code,
                Path: "",
                SeedNewProjectData: true));
            var api = await crdtProjectsService.OpenProject(crdtProject, scope.ServiceProvider);
            var morphTypes = await api.GetMorphTypes().ToArrayAsync();
            morphTypes.Should().HaveCount(CanonicalMorphTypes.All.Count,
                "morph types should have been seeded");
        }

        // Second open: morph types
        {
            await using var scope = host.Services.CreateAsyncScope();
            var crdtProjectsService = scope.ServiceProvider.GetRequiredService<CrdtProjectsService>();
            var crdtProject = crdtProjectsService.GetProject(code);
            crdtProject.Should().NotBeNull();
            var api = await crdtProjectsService.OpenProject(crdtProject, scope.ServiceProvider);
            // OpenProject calls MigrateDb(), which includes seeding morph types but only if they're not already seeded
            var morphTypes = await api.GetMorphTypes().ToArrayAsync();
            morphTypes.Should().HaveCount(CanonicalMorphTypes.All.Count,
                "morph types should not be duplicated");

            await using var dbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
            await dbContext.Database.EnsureDeletedAsync();
        }
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
