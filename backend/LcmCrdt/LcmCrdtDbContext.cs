using System.Text.Json;
using Crdt;
using Crdt.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

namespace LcmCrdt;

public class LcmCrdtDbContext(IOptions<CrdtConfig> options): DbContext, ICrdtDbContext
{
    public DbSet<ProjectData> ProjectData => Set<ProjectData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCrdt(options.Value);

        modelBuilder.Entity<ProjectData>().HasKey(p => p.Id);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<MiniLcm.MultiString>()
            .HaveColumnType("jsonb")
            .HaveConversion<MultiStringDbConverter>();
        builder.Properties<MiniLcm.WritingSystemId>()
            .HaveConversion<WritingSystemIdConverter>();
    }

    private class MultiStringDbConverter() : ValueConverter<MiniLcm.MultiString, string>(
        mul => JsonSerializer.Serialize(mul, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<MiniLcm.MultiString>(json, (JsonSerializerOptions?)null) ?? new());

    private class WritingSystemIdConverter() : ValueConverter<MiniLcm.WritingSystemId, string>(
        id => id.Code,
        code => new MiniLcm.WritingSystemId(code));
}
