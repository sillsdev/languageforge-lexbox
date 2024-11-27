using System.Linq.Expressions;
using System.Text.Json;
using SIL.Harmony;
using SIL.Harmony.Core;
using SIL.Harmony.Changes;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Objects;
using LcmCrdt.RemoteSync;
using LinqToDB;
using LinqToDB.AspNet.Logging;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using LinqToDB.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;
using SIL.Harmony.Db;

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
        services.AddSingleton<CrdtProjectsService>();

        services.AddHttpClient();
        services.AddSingleton<RefitSettings>(provider => new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new(JsonSerializerDefaults.Web)
            {
                TypeInfoResolver = provider.GetRequiredService<IOptions<CrdtConfig>>().Value
                    .MakeJsonTypeResolver()
            })
        });
        services.AddSingleton<CrdtHttpSyncService>();
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
                builder.HasMany(e => e.Components)
                        .WithOne()
                        .HasPrincipalKey(entry => entry.Id)
                        .HasForeignKey(c => c.ComplexFormEntryId)
                        .OnDelete(DeleteBehavior.Cascade);
                builder.HasMany(e => e.ComplexForms)
                    .WithOne()
                    .HasPrincipalKey(entry => entry.Id)
                    .HasForeignKey(c => c.ComponentEntryId)
                    .OnDelete(DeleteBehavior.Cascade);
                builder
                    .Property(e => e.ComplexFormTypes)
                    .HasColumnType("jsonb")
                    .HasConversion(list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<List<ComplexFormType>>(json,
                            (JsonSerializerOptions?)null) ?? new());
            })
            .Add<Sense>(builder =>
            {
                builder.HasMany<ComplexFormComponent>()
                    .WithOne()
                    .HasForeignKey(c => c.ComponentSenseId)
                    .OnDelete(DeleteBehavior.Cascade);
                builder.HasOne<Entry>()
                    .WithMany(e => e.Senses)
                    .HasForeignKey(sense => sense.EntryId);
                builder.Property(s => s.SemanticDomains)
                    .HasColumnType("jsonb")
                    .HasConversion(list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<List<SemanticDomain>>(json, (JsonSerializerOptions?)null) ?? new());
            })
            .Add<ExampleSentence>(builder =>
            {
                builder.HasOne<Sense>()
                    .WithMany(s => s.ExampleSentences)
                    .HasForeignKey(e => e.SenseId);
            })
            .Add<WritingSystem>(builder =>
            {
                builder.Property(w => w.Exemplars)
                    .HasColumnType("jsonb")
                    .HasConversion(list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<string[]>(json, (JsonSerializerOptions?)null) ??
                                Array.Empty<string>());
            })
            .Add<PartOfSpeech>()
            .Add<SemanticDomain>()
            .Add<ComplexFormType>()
            .Add<ComplexFormComponent>(builder =>
            {
                builder.ToTable("ComplexFormComponents");
            });

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
            .Add<DeleteChange<ComplexFormType>>()
            .Add<DeleteChange<ComplexFormComponent>>()
            .Add<SetPartOfSpeechChange>()
            .Add<AddSemanticDomainChange>()
            .Add<RemoveSemanticDomainChange>()
            .Add<ReplaceSemanticDomainChange>()
            .Add<CreateEntryChange>()
            .Add<CreateSenseChange>()
            .Add<CreateExampleSentenceChange>()
            .Add<CreatePartOfSpeechChange>()
            .Add<CreateSemanticDomainChange>()
            .Add<CreateWritingSystemChange>()
            .Add<AddComplexFormTypeChange>()
            .Add<AddEntryComponentChange>()
            .Add<RemoveComplexFormTypeChange>()
            .Add<SetComplexFormComponentChange>()
            .Add<CreateComplexFormType>();
    }

    public static Task<IMiniLcmApi> OpenCrdtProject(this IServiceProvider services, CrdtProject project)
    {
        //this method must not be async, otherwise Setting the project scope will not work as expected.
        //the project is stored in the async scope, if a new scope is created in this method then it will be gone once the method returns
        //making the lcm api unusable
        var projectsService = services.GetRequiredService<CrdtProjectsService>();
        projectsService.SetProjectScope(project);
        return LoadMiniLcmApi(services);
    }

    private static async Task<IMiniLcmApi> LoadMiniLcmApi(IServiceProvider services)
    {
        await services.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache();
        return services.GetRequiredService<IMiniLcmApi>();
    }
}
