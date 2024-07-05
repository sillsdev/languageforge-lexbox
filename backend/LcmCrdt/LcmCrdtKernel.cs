using System.Text.Json;
using Crdt;
using Crdt.Core;
using Crdt;
using Crdt.Changes;
using LcmCrdt.Changes;
using LcmCrdt.Objects;
using LinqToDB;
using LinqToDB.AspNet.Logging;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using LinqToDB.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LcmCrdt;


public static class LcmCrdtKernel
{
    public static IServiceCollection AddLcmCrdtClient(this IServiceCollection services)
    {
        LinqToDBForEFTools.Initialize();
        services.AddMemoryCache();

        services.AddCrdtData(
            ConfigureDbOptions,
            ConfigureCrdt
        );
        services.AddScoped<MiniLcm.ILexboxApi, CrdtLexboxApi>();
        services.AddScoped<CurrentProjectService>();
        services.AddSingleton<ProjectContext>();
        services.AddSingleton<ProjectsService>();
        return services;
    }

    private static void ConfigureDbOptions(IServiceProvider provider, DbContextOptionsBuilder builder)
    {
        var projectContext = provider.GetRequiredService<ProjectContext>();
        if (projectContext.Project is null) throw new NullReferenceException("Project is null");
        builder.UseSqlite($"Data Source={projectContext.Project.DbPath}")
            .UseLinqToDB(optionsBuilder =>
            {
                var mappingSchema = new MappingSchema();
                new FluentMappingBuilder(mappingSchema).HasAttribute<Commit>(new ColumnAttribute("DateTime",
                        nameof(Commit.HybridDateTime) + "." + nameof(HybridDateTime.DateTime)))
                    .HasAttribute<Commit>(new ColumnAttribute(nameof(HybridDateTime.Counter),
                        nameof(Commit.HybridDateTime) + "." + nameof(HybridDateTime.Counter)))
                    .Build();
                mappingSchema.SetConvertExpression((MiniLcm.WritingSystemId id) =>
                    new DataParameter { Value = id.Code, DataType = DataType.Text });
                optionsBuilder.AddMappingSchema(mappingSchema);
                var loggerFactory = provider.GetService<ILoggerFactory>();
                if (loggerFactory is not null)
                    optionsBuilder.AddCustomOptions(dataOptions => dataOptions.UseLoggerFactory(loggerFactory));
            });
    }

    private static void ConfigureCrdt(CrdtConfig config)
    {
        config.EnableProjectedTables = true;
        config.ObjectTypeListBuilder.AddDbModelConfig(builder =>
            {
                builder.Entity<ProjectData>().HasKey(p => p.Id);
                // builder.Owned<MultiString>();
            })
            .AddDbModelConvention(builder =>
            {
                builder.Properties<MiniLcm.MultiString>()
                    .HaveColumnType("jsonb")
                    .HaveConversion<MultiStringDbConverter>();
                builder.Properties<MiniLcm.WritingSystemId>()
                    .HaveConversion<WritingSystemIdConverter>();
            })
            .Add<Entry>(builder =>
            {
                // builder.OwnsOne(e => e.Note, n => n.ToJson());
                // builder.OwnsOne(e => e.LexemeForm, n => n.ToJson());
                // builder.OwnsOne(e => e.CitationForm, n => n.ToJson());
                // builder.OwnsOne(e => e.LiteralMeaning, n => n.ToJson());
            })
            .Add<Sense>(builder =>
            {
                builder.HasOne<Entry>()
                    .WithMany()
                    .HasForeignKey(sense => sense.EntryId);
                builder.Property(s => s.SemanticDomains)
                    .HasColumnType("jsonb")
                    .HasConversion(list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<List<MiniLcm.SemanticDomain>>(json, (JsonSerializerOptions?)null) ?? new());
            })
            .Add<ExampleSentence>(builder =>
            {
                builder.HasOne<Sense>()
                    .WithMany()
                    .HasForeignKey(e => e.SenseId);
            })
            .Add<WritingSystem>(builder =>
            {
                builder.Property(w => w.Exemplars)
                    .HasColumnType("jsonb")
                    .HasConversion(list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<string[]>(json, (JsonSerializerOptions?)null) ??
                                Array.Empty<string>());
            }).Add<PartOfSpeech>().Add<SemanticDomain>();

        config.ChangeTypeListBuilder.Add<JsonPatchChange<Entry>>()
            .Add<JsonPatchChange<Sense>>()
            .Add<JsonPatchChange<ExampleSentence>>()
            .Add<JsonPatchChange<WritingSystem>>()
            .Add<JsonPatchChange<PartOfSpeech>>()
            .Add<JsonPatchChange<SemanticDomain>>()
            .Add<DeleteChange<Entry>>()
            .Add<DeleteChange<Sense>>()
            .Add<DeleteChange<ExampleSentence>>()
            .Add<DeleteChange<WritingSystem>>()
            .Add<DeleteChange<PartOfSpeech>>()
            .Add<DeleteChange<SemanticDomain>>()
            .Add<SetPartOfSpeechChange>()
            .Add<AddSemanticDomainChange>()
            .Add<CreateEntryChange>()
            .Add<CreateSenseChange>()
            .Add<CreateExampleSentenceChange>()
            .Add<CreatePartOfSpeechChange>()
            .Add<CreateSemanticDomainChange>()
            .Add<CreateWritingSystemChange>();
    }


    private class MultiStringDbConverter() : ValueConverter<MiniLcm.MultiString, string>(
        mul => JsonSerializer.Serialize(mul, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<MiniLcm.MultiString>(json, (JsonSerializerOptions?)null) ?? new());

    private class WritingSystemIdConverter() : ValueConverter<MiniLcm.WritingSystemId, string>(
        id => id.Code,
        code => new MiniLcm.WritingSystemId(code));
}
