using System.Linq.Expressions;
using System.Text.Json;
using SIL.Harmony;
using SIL.Harmony.Linq2db;
using SIL.Harmony.Core;
using SIL.Harmony.Changes;
using LcmCrdt.Changes;
using LcmCrdt.Changes.CustomJsonPatches;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Changes.ExampleSentences;
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
using LcmCrdt.FullTextSearch;
using LcmCrdt.MediaServer;
using LcmCrdt.Project;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.Json.Serialization.Metadata;

namespace LcmCrdt;

public static class LcmCrdtKernel
{
    public static IServiceCollection AddLcmCrdtClient(this IServiceCollection services)
    {
        services.AddLcmCrdtClientCore();
        services.AddScoped<UpdateEntrySearchTableInterceptor>();
        services.AddScoped<EntrySearchServiceFactory>();
        return services;
    }

    public static IServiceCollection AddLcmCrdtClientCore(this IServiceCollection services)
    {
        AvoidTrimming();
        LinqToDBForEFTools.Initialize();


        services.AddMemoryCache();
        services.AddSingleton<IMiniLcmCultureProvider, LcmCrdtCultureProvider>();
        services.AddScoped<SnapshotAtCommitService>();
        services.AddSingleton<SetupCollationInterceptor>();
        services.AddDbContextFactory<LcmCrdtDbContext>(ConfigureDbOptions, ServiceLifetime.Scoped);
        services.RemoveAll<LcmCrdtDbContext>();//we don't want to be able to inject these directly as they will leak.
        services.AddOptions<LcmCrdtConfig>().BindConfiguration("LcmCrdt");

        services.AddCrdtDataDbFactory<LcmCrdtDbContext>(
            ConfigureCrdt
        );
        services.AddOptions<CrdtConfig>().PostConfigure((CrdtConfig crdtConfig, IOptions<LcmCrdtConfig> lcmConfig) =>
        {
            crdtConfig.LocalResourceCachePath = Path.Combine(lcmConfig.Value.ProjectPath, "localResourcesCache");
        });
        services.AddScoped<IMiniLcmApi, CrdtMiniLcmApi>();
        services.AddScoped<MiniLcmRepositoryFactory>();
        services.AddMiniLcmValidators();
        services.AddSingleton<ProjectDataCache>();
        services.AddScoped<CurrentProjectService>();
        services.AddScoped<HistoryService>();
        services.AddScoped<LcmMediaService>();
        services.AddScoped<SyncRepository>();
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
        services.AddSingleton<IRefitHttpServiceFactory, RefitHttpServiceFactory>();
        return services;
    }

    private static void AvoidTrimming()
    {
        //this is only here so that the compiler doesn't trim this method as Linq2Db uses it via reflection
        SqliteConnection.ClearAllPools();
    }

    public static void ConfigureDbOptions(IServiceProvider provider, DbContextOptionsBuilder builder)
    {
        var projectContext = provider.GetRequiredService<CurrentProjectService>();
        projectContext.ValidateProjectScope();
#if DEBUG
        builder.EnableSensitiveDataLogging();
#endif
        builder.EnableDetailedErrors();
        builder.UseSqlite($"Data Source={projectContext.Project.DbPath}")
            .UseLinqToDbCrdt(provider)
            .UseLinqToDB(optionsBuilder =>
            {
                var mappingSchema = new MappingSchema();
                new FluentMappingBuilder(mappingSchema).HasAttribute<Commit>(new ColumnAttribute("DateTime",
                        nameof(Commit.HybridDateTime) + "." + nameof(HybridDateTime.DateTime)))
                    .HasAttribute<Commit>(new ColumnAttribute(nameof(HybridDateTime.Counter),
                        nameof(Commit.HybridDateTime) + "." + nameof(HybridDateTime.Counter)))
                    //tells linq2db to rewrite Sense.SemanticDomains, into Json.Query(Sense.SemanticDomains)
                    .Entity<Sense>().Property(s => s.SemanticDomains).HasAttribute(new ExpressionMethodAttribute(SenseSemanticDomainsExpression()))
                    .Entity<RichString>().Member(r => r.GetPlainText()).IsExpression(r => Json.GetPlainText(r))
                    .Build();
                mappingSchema.SetConvertExpression((WritingSystemId id) =>
                    new DataParameter { Value = id.Code, DataType = DataType.Text });
                optionsBuilder.AddMappingSchema(mappingSchema);
                optionsBuilder.AddCustomOptions(options => options.UseSQLiteMicrosoft());
                var loggerFactory = provider.GetService<ILoggerFactory>();
                if (loggerFactory is not null)
                    optionsBuilder.AddCustomOptions(dataOptions => dataOptions.UseLoggerFactory(loggerFactory));
            });

        builder.AddInterceptors(new CustomSqliteFunctionInterceptor(), provider.GetRequiredService<SetupCollationInterceptor>());
        var updateSearchTableInterceptor = provider.GetService<UpdateEntrySearchTableInterceptor>();
        if (updateSearchTableInterceptor is not null)
            builder.AddInterceptors(updateSearchTableInterceptor);
    }

    private static Expression<Func<Sense, IQueryable<SemanticDomain>>> SenseSemanticDomainsExpression()
    {
        //using Sql.Property, otherwise if we used `s.SemanticDomains` again it would be recursively rewritten
        return s => Json.Query(Sql.Property<IList<SemanticDomain>>(s, nameof(Sense.SemanticDomains)));
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
                builder
                    .Property(e => e.PublishIn)
                    .HasColumnType("jsonb")
                    .HasConversion(list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                        json => string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<List<Publication>>(json,
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
                builder.HasOne<PartOfSpeech>(sense => sense.PartOfSpeech)
                    .WithMany()
                    .HasForeignKey(sense => sense.PartOfSpeechId)
                    .OnDelete(DeleteBehavior.SetNull);
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
                builder.Property(s => s.Translations)
                    .HasColumnType("jsonb")
                    .HasConversion(list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                        json => DeserializeTranslations(json));
            })
            .Add<WritingSystem>(builder =>
            {
                builder.HasIndex(ws => new { ws.WsId, ws.Type }).IsUnique();
                builder.Property(w => w.Exemplars)
                    .HasColumnType("jsonb")
                    .HasConversion(list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<string[]>(json, (JsonSerializerOptions?)null) ??
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

        config.AddRemoteResourceEntity();

        config.ChangeTypeListBuilder.Add<JsonPatchChange<Entry>>()
            .Add<JsonPatchChange<Sense>>()
            .Add<JsonPatchChange<WritingSystem>>()
            .Add<JsonPatchChange<PartOfSpeech>>()
            .Add<JsonPatchChange<SemanticDomain>>()
            .Add<JsonPatchChange<ComplexFormType>>()
            .Add<JsonPatchChange<Publication>>()
            .Add<DeleteChange<Entry>>()
            .Add<DeleteChange<Sense>>()
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

            //example sentence changes
            .Add<CreateExampleSentenceChange>()
            .Add<JsonPatchExampleSentenceChange>()
            .Add<Changes.SetOrderChange<ExampleSentence>>()
            .Add<DeleteChange<ExampleSentence>>()
            .Add<AddTranslationChange>()
            .Add<RemoveTranslationChange>()
            .Add<UpdateTranslationChange>()
            .Add<SetFirstTranslationIdChange>()

            .Add<CreatePartOfSpeechChange>()
            .Add<CreateSemanticDomainChange>()
            .Add<CreateWritingSystemChange>()
            .Add<CreatePublicationChange>()
            .Add<AddComplexFormTypeChange>()
            .Add<AddEntryComponentChange>()
            .Add<RemoveComplexFormTypeChange>()
            .Add<AddPublicationChange>()
            .Add<RemovePublicationChange>()
            .Add<ReplacePublicationChange>()
            .Add<SetComplexFormComponentChange>()
            .Add<CreateComplexFormType>()
            .Add<Changes.SetOrderChange<Sense>>()
            .Add<Changes.SetOrderChange<ComplexFormComponent>>()
            .Add<Changes.SetOrderChange<WritingSystem>>()
            // When adding anything other than a Delete or JsonPatch change,
            // you must add an instance of it to UseChangesTests.GetAllChanges()
            ;

        config.JsonSerializerOptions.TypeInfoResolver =
            (config.JsonSerializerOptions.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
            .WithAddedModifier(Json.ExampleSentenceTranslationModifier);
    }

    public static IEnumerable<Type> AllChangeTypes()
    {
        var crdtConfig = new CrdtConfig();
        ConfigureCrdt(crdtConfig);
        return crdtConfig.ChangeTypes;
    }

    public static IEnumerable<Type> AllObjectTypes()
    {
        var crdtConfig = new CrdtConfig();
        ConfigureCrdt(crdtConfig);
        return crdtConfig.ObjectTypes;
    }

    private static IList<Translation> DeserializeTranslations(string json)
    {
        //in the db Translations may be a list, or they could be a json object, so we need to deserialize it differently
        var deserializationTarget = JsonSerializer.Deserialize<DbTranslationDeserializationTarget>(json);
        return deserializationTarget?.GetTranslations() ?? [];
    }

    public static async Task<IMiniLcmApi> OpenCrdtProject(this IServiceProvider services, CrdtProject project)
    {
        await services.GetRequiredService<CurrentProjectService>().SetupProjectContext(project);
        return services.GetRequiredService<IMiniLcmApi>();
    }
}
