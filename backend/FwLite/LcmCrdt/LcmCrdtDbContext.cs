using System.Text.Json;
using LcmCrdt.FullTextSearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using SIL.Harmony;
using SIL.Harmony.Db;

namespace LcmCrdt;

public class LcmCrdtDbContext : DbContext, ICrdtDbContext
{
    private readonly IOptions<CrdtConfig> _crdtOptions;

    public LcmCrdtDbContext(
        DbContextOptions<LcmCrdtDbContext> dbContextOptions,
        IOptions<CrdtConfig> options) : base(dbContextOptions)
    {
        _crdtOptions = options;
        ChangeTracker.Tracked += OnTracked;
    }

    public DbSet<ProjectData> ProjectData => Set<ProjectData>();
    public IQueryable<WritingSystem> WritingSystems => Set<WritingSystem>().AsNoTracking();
    public IQueryable<WritingSystem> WritingSystemsOrdered => Set<WritingSystem>().AsNoTracking()
        .OrderBy(ws => ws.Order).ThenBy(ws => ws.Id);
    public IQueryable<Entry> Entries => Set<Entry>().AsNoTracking();
    public IQueryable<ComplexFormComponent> ComplexFormComponents => Set<ComplexFormComponent>().AsNoTracking();
    public IQueryable<ComplexFormType> ComplexFormTypes => Set<ComplexFormType>().AsNoTracking();
    public IQueryable<Sense> Senses => Set<Sense>().AsNoTracking();
    public IQueryable<ExampleSentence> ExampleSentences => Set<ExampleSentence>().AsNoTracking();
    public IQueryable<SemanticDomain> SemanticDomains => Set<SemanticDomain>().AsNoTracking();
    public IQueryable<PartOfSpeech> PartsOfSpeech => Set<PartOfSpeech>().AsNoTracking();
    public IQueryable<Publication> Publications => Set<Publication>().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCrdt(_crdtOptions.Value);

        var projectDataModel = modelBuilder.Entity<ProjectData>();
        projectDataModel.HasKey(p => p.Id);
        projectDataModel.Ignore(p => p.ServerId);
        //setting default value to handle migration
        projectDataModel.Property(p => p.Role).HasConversion<EnumToStringConverter<UserProjectRole>>().HasDefaultValue(UserProjectRole.Editor);

        var entrySearchModel = modelBuilder.Entity<EntrySearchRecord>();
        entrySearchModel.ToTable(nameof(EntrySearchRecord), tb => tb.ExcludeFromMigrations());
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

    private void OnTracked(object? sender, EntityTrackedEventArgs e)
    {
        // When navigation properties (like Sense.PartOfSpeech) are set and projected tables are enabled,
        // EF Core may track the referenced entity and try to insert it into the projected table.
        // These entities already exist in the database, so we detach them to prevent duplicate inserts.
        if (e.Entry.State == EntityState.Added && e.FromQuery == false)
        {
            if (e.Entry.Entity is PartOfSpeech or SemanticDomain or ComplexFormType)
            {
                // Check if we're projecting snapshots by looking for other entities (like Sense) being added
                // If a Sense is being added and it has a navigation property to this entity, we should detach
                var hasReferencingEntity = ChangeTracker.Entries()
                    .Any(entry => entry != e.Entry && 
                          entry.State == EntityState.Added && 
                          (entry.Entity is Sense || entry.Entity is Entry));
                
                if (hasReferencingEntity)
                {
                    e.Entry.State = EntityState.Detached;
                }
            }
        }
    }
}
