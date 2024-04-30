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
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        services.AddScoped<ILexboxApi, CrdtLexboxApi>();
        services.AddSingleton<IHostedService, StartupService>();
        services.AddOptions<JsonOptions>().PostConfigure<IOptions<CrdtConfig>>((jsonOptions, crdtConfig) =>
        {
            jsonOptions.SerializerOptions.TypeInfoResolver = crdtConfig.Value.MakeJsonTypeResolver();
        });
        return services;
    }

    private class MultiStringDbConverter() : ValueConverter<MultiString, string>(
        mul => JsonSerializer.Serialize(mul, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<MultiString>(json, (JsonSerializerOptions?)null) ?? new());

    private class StartupService(IServiceProvider services) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var serviceScope = services.CreateScope();
            var crdtDbContext = serviceScope.ServiceProvider.GetRequiredService<CrdtDbContext>();
            //todo use migrations before releasing
            // await dbContext.Database.MigrateAsync(cancellationToken);
            await crdtDbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
