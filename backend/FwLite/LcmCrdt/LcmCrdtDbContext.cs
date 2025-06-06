using System.Text.Json;
using LcmCrdt.Data;
using LcmCrdt.FullTextSearch;
using SIL.Harmony;
using SIL.Harmony.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

namespace LcmCrdt;

public class LcmCrdtDbContext(DbContextOptions<LcmCrdtDbContext> dbContextOptions, IOptions<CrdtConfig> options, SetupCollationInterceptor setupCollationInterceptor)
    : DbContext(dbContextOptions), ICrdtDbContext
{
    public DbSet<ProjectData> ProjectData => Set<ProjectData>();
    public IQueryable<WritingSystem> WritingSystems => Set<WritingSystem>().AsNoTracking();
    public DbSet<EntrySearchRecord> EntrySearchRecords => Set<EntrySearchRecord>();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(setupCollationInterceptor, new CustomSqliteFunctionInterceptor());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCrdt(options.Value);

        var projectDataModel = modelBuilder.Entity<ProjectData>();
        projectDataModel.HasKey(p => p.Id);
        projectDataModel.Ignore(p => p.ServerId);
        //setting default value to handle migration
        projectDataModel.Property(p => p.Role).HasConversion<EnumToStringConverter<UserProjectRole>>().HasDefaultValue(UserProjectRole.Editor);

        var entrySearchModel = modelBuilder.Entity<EntrySearchRecord>();
        entrySearchModel.ToTable(nameof(EntrySearchRecord));
        entrySearchModel.HasKey(e => e.RowId);
        entrySearchModel.Property(e => e.Match).HasColumnName(nameof(EntrySearchRecord));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<MultiString>()
            .HaveColumnType("jsonb")
            .HaveConversion<MultiStringDbConverter>();
        builder.Properties<RichString?>()
            .HaveColumnType("jsonb")
            .HaveConversion<RichStringDbConverter>();
        builder.Properties<RichMultiString>()
            .HaveColumnType("jsonb")
            .HaveConversion<RichMultiStringDbConverter>();
        builder.Properties<WritingSystemId>()
            .HaveConversion<WritingSystemIdConverter>();
    }

    private class MultiStringDbConverter() : ValueConverter<MultiString, string>(
        mul => JsonSerializer.Serialize(mul, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<MultiString>(json, (JsonSerializerOptions?)null) ?? new());
    private class RichStringDbConverter() : ValueConverter<RichString?, string?>(
        richString => richString == null ? null : JsonSerializer.Serialize(richString, (JsonSerializerOptions?)null),
        json => Deserialize(json))
    {
        //old data may be just a string, so we need to handle that
        private static RichString? Deserialize(string? maybeJson)
        {
            if (maybeJson is null) return null;
            if (maybeJson.StartsWith('[') || maybeJson.StartsWith('{'))
            {
                try
                {
                    return JsonSerializer.Deserialize<RichString?>(maybeJson);
                }
                catch { }
            }
            return new RichString(maybeJson);
        }
    }

    private class RichMultiStringDbConverter() : ValueConverter<RichMultiString, string>(
        mul => JsonSerializer.Serialize(mul, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<RichMultiString>(json, (JsonSerializerOptions?)null) ?? new());

    private class WritingSystemIdConverter() : ValueConverter<WritingSystemId, string>(
        id => id.Code,
        code => new WritingSystemId(code));
}
