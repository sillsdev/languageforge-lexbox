using SIL.Harmony.Config;
using System.ComponentModel;
using System.Text.Json;
using LcmCrdt.Data;
using LcmCrdt.FullTextSearch;
using SIL.Harmony;
using SIL.Harmony.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

namespace LcmCrdt;

public class LcmCrdtDbContext(
    DbContextOptions<LcmCrdtDbContext> dbContextOptions,
    IOptions<HarmonyConfig> options
    )
    : DbContext(dbContextOptions), ICrdtDbContext
{
    public DbSet<ProjectData> ProjectData => Set<ProjectData>();
    public IQueryable<WritingSystem> WritingSystems => Set<WritingSystem>().AsNoTracking();
    public IQueryable<WritingSystem> WritingSystemsOrdered => Set<WritingSystem>().AsNoTracking()
        .OrderBy(ws => ws.Order).ThenBy(ws => ws.Id);
    public IQueryable<Entry> Entries => Set<Entry>().AsNoTracking();
    public IQueryable<ComplexFormComponent> ComplexFormComponents => Set<ComplexFormComponent>().AsNoTracking();
    public IQueryable<ComplexFormType> ComplexFormTypes => Set<ComplexFormType>().AsNoTracking();
    public IQueryable<MorphType> MorphTypes => Set<MorphType>().AsNoTracking();
    public IQueryable<Sense> Senses => Set<Sense>().AsNoTracking();
    public IQueryable<ExampleSentence> ExampleSentences => Set<ExampleSentence>().AsNoTracking();
    public IQueryable<SemanticDomain> SemanticDomains => Set<SemanticDomain>().AsNoTracking();
    public IQueryable<PartOfSpeech> PartsOfSpeech => Set<PartOfSpeech>().AsNoTracking();
    public IQueryable<Publication> Publications => Set<Publication>().AsNoTracking();
    public IQueryable<CustomView> CustomViews => Set<CustomView>().AsNoTracking();
    public IQueryable<CommentThread> CommentThreads => Set<CommentThread>().AsNoTracking();
    public IQueryable<UserComment> UserComments => Set<UserComment>().AsNoTracking();
    public DbSet<UnreadComment> UnreadComments => Set<UnreadComment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCrdt(options.Value);

        var projectDataModel = modelBuilder.Entity<ProjectData>();
        projectDataModel.HasKey(p => p.Id);
        projectDataModel.Ignore(p => p.ServerId);
        //setting default value to handle migration
        projectDataModel.Property(p => p.Role).HasConversion<EnumToStringConverter<UserProjectRole>>().HasDefaultValue(UserProjectRole.Editor);

        var entrySearchModel = modelBuilder.Entity<EntrySearchRecord>();
        entrySearchModel.ToTable(nameof(EntrySearchRecord), tb => tb.ExcludeFromMigrations());

        var morphTypeModel = modelBuilder.Entity<MorphType>();
        morphTypeModel.HasIndex(m => m.Kind).IsUnique();

        var unreadCommentModel = modelBuilder.Entity<UnreadComment>();
        unreadCommentModel.HasKey(c => c.CommentId);
        unreadCommentModel.HasIndex(c => c.CommentThreadId);

        var senseModel = modelBuilder.Entity<Sense>();
        senseModel.Property(s => s.Pictures).HasColumnType("jsonb").HasDefaultValueSql("'[]'");
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
        builder.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetDbConverter>();
        builder.Properties<List<Picture>>()
            .HaveConversion<PictureListDbConverter>();
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

    private class DateTimeOffsetDbConverter() : ValueConverter<DateTimeOffset, DateTime>(
        d => d.UtcDateTime,
        //need to use ticks here because the DateTime is stored as UTC, but the db records it as unspecified
        d => new DateTimeOffset(d.Ticks, TimeSpan.Zero));

    private class PictureListDbConverter() : ValueConverter<List<Picture>, string>(
        pic => JsonSerializer.Serialize(pic, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<List<Picture>>(json, (JsonSerializerOptions?)null) ?? new());
}
