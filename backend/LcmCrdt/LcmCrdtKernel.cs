using System.Diagnostics;
using System.Text.Json;
using CrdtLib;
using CrdtLib.Changes;
using CrdtLib.Db;
using CrdtLib.Entities;
using CrdtLib.Helpers;
using LcmCrdt.Changes;
using MiniLcm;
using LinqToDB;
using LinqToDB.AspNet.Logging;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using LinqToDB.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LcmCrdt;

using Entry = Objects.Entry;
using ExampleSentence = Objects.ExampleSentence;
using Sense = Objects.Sense;

public static class LcmCrdtKernel
{
    public static IServiceCollection AddLcmCrdtClient(this IServiceCollection services,
        string dbPath,
        ILoggerFactory? loggerFactory = null)
    {
        LinqToDBForEFTools.Initialize();

        services.AddCrdtData(
            builder => builder.UseSqlite($"Data Source={dbPath}").UseLinqToDB(optionsBuilder =>
            {
                var mappingSchema = new MappingSchema();
                new FluentMappingBuilder(mappingSchema)
                    .HasAttribute<Commit>(new ColumnAttribute("DateTime",
                        nameof(Commit.HybridDateTime) + "." + nameof(HybridDateTime.DateTime)))
                    .HasAttribute<Commit>(new ColumnAttribute(nameof(HybridDateTime.Counter),
                        nameof(Commit.HybridDateTime) + "." + nameof(HybridDateTime.Counter)))
                    .Build();
                mappingSchema.SetConvertExpression((WritingSystemId id) => new DataParameter
                {
                    Value = id.Code, DataType = DataType.Text
                });
                optionsBuilder.AddMappingSchema(mappingSchema);
                if (loggerFactory is not null)
                    optionsBuilder.AddCustomOptions(dataOptions => dataOptions.UseLoggerFactory(loggerFactory));
            }),
            config =>
            {
                config.EnableProjectedTables = true;
                config.ObjectTypeListBuilder.AddDbModelConfig(builder =>
                    {
                        // builder.Owned<MultiString>();
                    })
                    .AddDbModelConvention(builder =>
                    {
                        builder.Properties<MultiString>()
                            .HaveColumnType("jsonb")
                            .HaveConversion<MultiStringDbConverter>();
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
                        builder.Property(s => s.SemanticDomain)
                            .HasColumnType("jsonb")
                            .HasConversion(list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ??
                                        new());
                    })
                    .Add<ExampleSentence>(builder =>
                    {
                        builder.HasOne<Sense>()
                            .WithMany()
                            .HasForeignKey(e => e.SenseId);
                    });

                config.ChangeTypeListBuilder.Add<JsonPatchChange<Entry>>()
                    .Add<JsonPatchChange<Sense>>()
                    .Add<JsonPatchChange<ExampleSentence>>()
                    .Add<DeleteChange<Entry>>()
                    .Add<DeleteChange<Sense>>()
                    .Add<DeleteChange<ExampleSentence>>()
                    .Add<CreateEntryChange>()
                    .Add<CreateSenseChange>()
                    .Add<CreateExampleSentenceChange>();
            }
        );
        services.AddSingleton<ILexboxApi, CrdtLexboxApi>();
        services.AddSingleton<IHostedService, StartupService>();
        return services;
    }

    private class MultiStringDbConverter() : ValueConverter<MultiString, string>(
        mul => JsonSerializer.Serialize(mul, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<MultiString>(json, (JsonSerializerOptions?)null) ?? new());

    private class StartupService(CrdtDbContext dbContext, IServiceProvider services) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //todo use migrations before releasing
            // await dbContext.Database.MigrateAsync(cancellationToken);
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            var lexboxApi = services.GetRequiredService<ILexboxApi>();
            var id = new Guid("c7328f18-118a-4f83-9d88-c408778b7f63");
            if (await lexboxApi.GetEntry(id) is not null) return;
            await lexboxApi.CreateEntry(new()
            {
                Id = id,
                LexemeForm = { Values = { { "en", "Kevin" } } },
                Note = { Values = { { "en", "this is a test note from Kevin" } } },
                CitationForm = { Values = { { "en", "Kevin" } } },
                LiteralMeaning = { Values = { { "en", "Kevin" } } },
                Senses =
                [
                    new()
                    {
                        Gloss = { Values = { { "en", "Kevin" } } },
                        Definition = { Values = { { "en", "Kevin" } } },
                        SemanticDomain = ["Person"],
                        ExampleSentences =
                        [
                            new() { Sentence = { Values = { { "en", "Kevin is a good guy" } } } }
                        ]
                    }
                ]
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
