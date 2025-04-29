using System.Text.Json;
using LcmCrdt.CompiledModels;
using LcmCrdt.Data;
using SIL.Harmony;
using SIL.Harmony.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.Extensions.Options;

namespace LcmCrdt;

public class LcmCrdtDbContext(DbContextOptions<LcmCrdtDbContext> dbContextOptions, IOptions<CrdtConfig> options, SetupCollationInterceptor setupCollationInterceptor)
    : DbContext(dbContextOptions), ICrdtDbContext
{
    public DbSet<ProjectData> ProjectData => Set<ProjectData>();
    public IQueryable<WritingSystem> WritingSystems => Set<WritingSystem>().AsNoTracking();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(setupCollationInterceptor);
        optionsBuilder.UseModel(LcmCrdtDbContextModel.Instance);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCrdt(options.Value);

        var projectDataModel = modelBuilder.Entity<ProjectData>();
        projectDataModel.HasKey(p => p.Id);
        projectDataModel.Ignore(p => p.ServerId);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<MultiString>()
            .HaveColumnType("jsonb")
            .HaveConversion<MultiStringDbConverter>();
        builder.Properties<RichMultiString>()
            .HaveColumnType("jsonb")
            .HaveConversion<RichMultiStringDbConverter>();
        builder.Properties<WritingSystemId>()
            .HaveConversion<WritingSystemIdConverter>();
    }

    internal class MultiStringDbConverter() : ValueConverter<MultiString, string>(
        mul => Serialize(mul),
        json => Deserialize<MultiString>(json) ?? new());

    internal class RichMultiStringDbConverter() : ValueConverter<RichMultiString, string>(
        mul => Serialize(mul),
        json => Deserialize<RichMultiString>(json) ?? new());

    internal class WritingSystemIdConverter() : ValueConverter<WritingSystemId, string>(
        id => id.Code,
        code => new WritingSystemId(code));

    internal static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, typeof(T), JsonSourceGenerationContext.Default);
    }

    internal static T Deserialize<T>(string value) where T : class
    {
        return JsonSerializer.Deserialize(value, typeof(T), JsonSourceGenerationContext.Default) as T ??
               throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");
    }
}
