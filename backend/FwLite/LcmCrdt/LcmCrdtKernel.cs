using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SIL.Harmony;
using SIL.Harmony.Core;
using SIL.Harmony.Changes;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Data;
using LcmCrdt.Objects;
using LcmCrdt.RemoteSync;
using LinqToDB;
using LinqToDB.AspNet.Logging;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using LinqToDB.Mapping;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm.Project;
using MiniLcm.Validators;
using Refit;
using MiniLcm.Culture;
using LcmCrdt.Culture;
using MiniLcm.Filtering;

namespace LcmCrdt;

public static class LcmCrdtKernel
{
    public static IServiceCollection AddLcmCrdtClient(this IServiceCollection services)
    {
        AvoidTrimming();
        LinqToDBForEFTools.Initialize();


        services.AddMemoryCache();
        services.AddSingleton<IMiniLcmCultureProvider, LcmCrdtCultureProvider>();
        services.AddSingleton<SetupCollationInterceptor>();
        services.AddDbContext<LcmCrdtDbContext>(ConfigureDbOptions);
        services.AddOptions<LcmCrdtConfig>().BindConfiguration("LcmCrdt");

        services.AddCrdtData<LcmCrdtDbContext>(
            ConfigureCrdt
        );
        services.AddScoped<IMiniLcmApi, CrdtMiniLcmApi>();
        services.AddMiniLcmValidators();
        services.AddScoped<CurrentProjectService>();
        services.AddScoped<HistoryService>();
        services.AddSingleton<CrdtProjectsService>();
        services.AddSingleton<IProjectProvider>(s => s.GetRequiredService<CrdtProjectsService>());

        services.AddHttpClient();
        services.AddSingleton(provider => new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new(JsonSerializerDefaults.Web)
            {
                TypeInfoResolver = provider.GetRequiredService<IOptions<CrdtConfig>>().Value
                    .MakeLcmCrdtExternalJsonTypeResolver()
            })
        });
        services.AddSingleton<CrdtHttpSyncService>();
        return services;
    }

    private static void AvoidTrimming()
    {
        //this is only here so that the compiler doesn't trim this method as Linq2Db uses it via reflection
        SqliteConnection.ClearAllPools();
    }

    private static void ConfigureDbOptions(IServiceProvider provider, DbContextOptionsBuilder builder)
    {
        var projectContext = provider.GetRequiredService<CurrentProjectService>();
        projectContext.ValidateProjectScope();
#if DEBUG
        builder.EnableSensitiveDataLogging();
#endif
        builder.EnableDetailedErrors();
        builder.UseSqlite($"Data Source={projectContext.Project.DbPath}")
            .UseLinqToDB(optionsBuilder =>
            {
                var mappingSchema = new MappingSchema();
                new FluentMappingBuilder(mappingSchema).HasAttribute<Commit>(new ColumnAttribute("DateTime",
                        nameof(Commit.HybridDateTime) + "." + nameof(HybridDateTime.DateTime)))
                    .HasAttribute<Commit>(new ColumnAttribute(nameof(HybridDateTime.Counter),
                        nameof(Commit.HybridDateTime) + "." + nameof(HybridDateTime.Counter)))
                    //tells linq2db to rewrite Sense.SemanticDomains, into Json.Query(Sense.SemanticDomains)
                    .Entity<Sense>().Property(s => s.SemanticDomains).HasAttribute(new ExpressionMethodAttribute(SenseSemanticDomainsExpression()))
                    //for some reason since we started using compiled models linq2db is not picking up the conversion from EF
                    //so we need to define it here also
                    .HasConversion(
                        list => LcmCrdtDbContext.Serialize(list),
                        json => LcmCrdtDbContext.Deserialize<List<SemanticDomain>>(json) ?? new()
                        )
                    .Entity<Entry>()
                    .Property(e => e.ComplexFormTypes).HasConversion(list => LcmCrdtDbContext.Serialize(list),
                        json => LcmCrdtDbContext.Deserialize<List<ComplexFormType>>(json) ?? new())
                    .Property(e => e.PublishIn).HasConversion(list => LcmCrdtDbContext.Serialize(list),
                        json => string.IsNullOrWhiteSpace(json)
                            ? new()
                            : LcmCrdtDbContext.Deserialize<List<Publication>>(json) ?? new())
                    .Entity<WritingSystem>()
                    .Property(w => w.Exemplars)
                    .HasConversion(list => LcmCrdtDbContext.Serialize(list),
                        json => LcmCrdtDbContext.Deserialize<string[]>(json) ??
                                Array.Empty<string>())
                    .Build();
                mappingSchema.SetConvertExpression((WritingSystemId id) =>
                    new DataParameter { Value = id.Code, DataType = DataType.Text });
                optionsBuilder.AddMappingSchema(mappingSchema);
                optionsBuilder.AddCustomOptions(options => options.UseSQLiteMicrosoft());
                var loggerFactory = provider.GetService<ILoggerFactory>();
                if (loggerFactory is not null)
                    optionsBuilder.AddCustomOptions(dataOptions => dataOptions.UseLoggerFactory(loggerFactory));
            });
    }

    private static Expression<Func<Sense, IQueryable<SemanticDomain>>> SenseSemanticDomainsExpression()
    {
        //using Sql.Property, otherwise if we used `s.SemanticDomains` again it would be recursively rewritten
        return s => Json.Query(Sql.Property<IList<SemanticDomain>>(s, nameof(Sense.SemanticDomains)));
    }

    public static CrdtConfig MakeConfig()
    {
        var config = new CrdtConfig();
        ConfigureCrdt(config);
        return config;
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
                    .HasConversion(list => LcmCrdtDbContext.Serialize(list),
                        json => LcmCrdtDbContext.Deserialize<List<ComplexFormType>>(json) ?? new());
                builder
                    .Property(e => e.PublishIn)
                    .HasColumnType("jsonb")
                    .HasConversion(list => LcmCrdtDbContext.Serialize(list),
                        json => string.IsNullOrWhiteSpace(json) ? new() : LcmCrdtDbContext.Deserialize<List<Publication>>(json) ?? new());
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
                    .HasConversion(list => LcmCrdtDbContext.Serialize(list),
                        json => LcmCrdtDbContext.Deserialize<List<SemanticDomain>>(json) ?? new());
            })
            .Add<ExampleSentence>(builder =>
            {
                builder.HasOne<Sense>()
                    .WithMany(s => s.ExampleSentences)
                    .HasForeignKey(e => e.SenseId);
            })
            .Add<WritingSystem>(builder =>
            {
                builder.Property(ws => ws.WsId).HasSentinel(new WritingSystemId("default"));
                builder.HasIndex(ws => new { ws.WsId, ws.Type }).IsUnique();
                builder.Property(w => w.Exemplars)
                    .HasColumnType("jsonb")
                    .HasConversion(list => LcmCrdtDbContext.Serialize(list),
                        json => LcmCrdtDbContext.Deserialize<string[]>(json) ??
                                Array.Empty<string>());
            })
            .Add<PartOfSpeech>()
            .Add<Publication>()
            .Add<SemanticDomain>()
            .Add<ComplexFormType>()
            .Add<ComplexFormComponent>(builder =>
            {
                const string componentSenseId = "ComponentSenseId";
                builder.ToTable("ComplexFormComponents");
                builder.Property(c => c.ComponentSenseId).HasColumnName(componentSenseId);
                //these indexes are used to ensure that we don't create duplicate complex form components
                //we need the filter otherwise 2 components which are the same and have a null sense id can be created because 2 rows with the same null are not considered duplicates
                builder.HasIndex(component => new
                {
                    component.ComplexFormEntryId,
                    component.ComponentEntryId,
                    component.ComponentSenseId
                }).IsUnique().HasFilter($"{componentSenseId} IS NOT NULL");
                builder.HasIndex(component => new
                {
                    component.ComplexFormEntryId,
                    component.ComponentEntryId
                }).IsUnique().HasFilter($"{componentSenseId} IS NULL");
            });

        config.ChangeTypeListBuilder.Add<JsonPatchChange<Entry>>()
            .Add<JsonPatchChange<Sense>>()
            .Add<JsonPatchChange<ExampleSentence>>()
            .Add<JsonPatchChange<WritingSystem>>()
            .Add<JsonPatchChange<PartOfSpeech>>()
            .Add<JsonPatchChange<SemanticDomain>>()
            .Add<JsonPatchChange<ComplexFormType>>()
            .Add<JsonPatchChange<Publication>>()
            .Add<DeleteChange<Entry>>()
            .Add<DeleteChange<Sense>>()
            .Add<DeleteChange<ExampleSentence>>()
            .Add<DeleteChange<WritingSystem>>()
            .Add<DeleteChange<PartOfSpeech>>()
            .Add<DeleteChange<SemanticDomain>>()
            .Add<DeleteChange<ComplexFormType>>()
            .Add<DeleteChange<ComplexFormComponent>>()
            .Add<DeleteChange<Publication>>()
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
            .Add<CreatePublicationChange>()
            .Add<AddComplexFormTypeChange>()
            .Add<AddEntryComponentChange>()
            .Add<RemoveComplexFormTypeChange>()
            .Add<SetComplexFormComponentChange>()
            .Add<CreateComplexFormType>()
            .Add<Changes.SetOrderChange<Sense>>()
            .Add<Changes.SetOrderChange<ExampleSentence>>()
            .Add<Changes.SetOrderChange<ComplexFormComponent>>()
            // When adding anything other than a Delete or JsonPatch change,
            // you must add an instance of it to UseChangesTests.GetAllChanges()
            ;
    }

    public static IEnumerable<Type> AllChangeTypes()
    {
        var crdtConfig = new CrdtConfig();
        ConfigureCrdt(crdtConfig);
        return crdtConfig.ChangeTypes;
    }


    public static async Task<IMiniLcmApi> OpenCrdtProject(this IServiceProvider services, CrdtProject project)
    {
        await services.GetRequiredService<CurrentProjectService>().SetupProjectContext(project);
        return services.GetRequiredService<IMiniLcmApi>();
    }
}
