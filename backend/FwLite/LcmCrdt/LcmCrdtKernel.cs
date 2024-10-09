﻿using System.Text.Json;
using SIL.Harmony;
using SIL.Harmony.Core;
using SIL.Harmony.Changes;
using LcmCrdt.Changes;
using LcmCrdt.Objects;
using LinqToDB;
using LinqToDB.AspNet.Logging;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using LinqToDB.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LcmCrdt;

public static class LcmCrdtKernel
{
    public static IServiceCollection AddLcmCrdtClient(this IServiceCollection services)
    {
        LinqToDBForEFTools.Initialize();
        services.AddMemoryCache();
        services.AddDbContext<LcmCrdtDbContext>(ConfigureDbOptions);
        services.AddOptions<LcmCrdtConfig>().BindConfiguration("LcmCrdt");

        services.AddCrdtData<LcmCrdtDbContext>(
            ConfigureCrdt
        );
        services.AddScoped<IMiniLcmApi, CrdtMiniLcmApi>();
        services.AddScoped<CurrentProjectService>();
        services.AddSingleton<ProjectContext>();
        services.AddSingleton<ProjectsService>();
        return services;
    }

    private static void ConfigureDbOptions(IServiceProvider provider, DbContextOptionsBuilder builder)
    {
        var projectContext = provider.GetRequiredService<ProjectContext>();
        if (projectContext.Project is null) throw new NullReferenceException("Project is null");
#if DEBUG
        builder.EnableSensitiveDataLogging();
#endif
        builder.UseSqlite($"Data Source={projectContext.Project.DbPath}")
            .UseLinqToDB(optionsBuilder =>
            {
                var mappingSchema = new MappingSchema();
                new FluentMappingBuilder(mappingSchema).HasAttribute<Commit>(new ColumnAttribute("DateTime",
                        nameof(Commit.HybridDateTime) + "." + nameof(HybridDateTime.DateTime)))
                    .HasAttribute<Commit>(new ColumnAttribute(nameof(HybridDateTime.Counter),
                        nameof(Commit.HybridDateTime) + "." + nameof(HybridDateTime.Counter)))
                    .Entity<Entry>().Property(e => e.Id)
                    .Association(e => (e.Senses as IEnumerable<Sense>)!, e => e.Id, s => s.EntryId)
                    .Build();
                mappingSchema.SetConvertExpression((WritingSystemId id) =>
                    new DataParameter { Value = id.Code, DataType = DataType.Text });
                optionsBuilder.AddMappingSchema(mappingSchema);
                var loggerFactory = provider.GetService<ILoggerFactory>();
                if (loggerFactory is not null)
                    optionsBuilder.AddCustomOptions(dataOptions => dataOptions.UseLoggerFactory(loggerFactory));
            });
    }

    public static void ConfigureCrdt(CrdtConfig config)
    {
        config.EnableProjectedTables = true;
        config.ObjectTypeListBuilder
            .CustomAdapter<IObjectWithId, MiniLcmCrdtAdapter>()
            .Add<Entry>(builder =>
            {
                builder.Ignore(e => e.Senses);
                // builder.OwnsOne(e => e.Note, n => n.ToJson());
                // builder.OwnsOne(e => e.LexemeForm, n => n.ToJson());
                // builder.OwnsOne(e => e.CitationForm, n => n.ToJson());
                // builder.OwnsOne(e => e.LiteralMeaning, n => n.ToJson());
            })
            .Add<Sense>(builder =>
            {
                builder.Ignore(s => s.ExampleSentences);
                builder.HasOne<Entry>()
                    .WithMany()
                    .HasForeignKey(sense => sense.EntryId);
                builder.Property(s => s.SemanticDomains)
                    .HasColumnType("jsonb")
                    .HasConversion(list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<List<SemanticDomain>>(json, (JsonSerializerOptions?)null) ?? new());
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
            .Add<RemoveSemanticDomainChange>()
            .Add<ReplaceSemanticDomainChange>()
            .Add<CreateEntryChange>()
            .Add<CreateSenseChange>()
            .Add<CreateExampleSentenceChange>()
            .Add<CreatePartOfSpeechChange>()
            .Add<CreateSemanticDomainChange>()
            .Add<CreateWritingSystemChange>();
    }
}
