using System.Data.Common;
using System.Text.Json;
using LcmCrdt.Data;
using Microsoft.Data.Sqlite;
using SIL.Harmony;
using SIL.Harmony.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCrdt(options.Value);

        modelBuilder.Entity<ProjectData>().HasKey(p => p.Id);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<MultiString>()
            .HaveColumnType("jsonb")
            .HaveConversion<MultiStringDbConverter>();
        builder.Properties<WritingSystemId>()
            .HaveConversion<WritingSystemIdConverter>();
    }

    private class MultiStringDbConverter() : ValueConverter<MultiString, string>(
        mul => JsonSerializer.Serialize(mul, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<MultiString>(json, (JsonSerializerOptions?)null) ?? new());

    private class WritingSystemIdConverter() : ValueConverter<WritingSystemId, string>(
        id => id.Code,
        code => new WritingSystemId(code));
}
