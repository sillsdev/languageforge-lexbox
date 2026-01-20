using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MiniLcm;
using MiniLcm.Filtering;
using MiniLcm.Models;
using MiniLcm.Project;
using MiniLcm.Validators;

namespace FwLiteWeb.Routes;

public static class MiniLcmRoutes
{
    public static void AddMiniLcmRouteServices(this IServiceCollection services)
    {
        services.AddTransient<MiniLcmHolder>();
    }

    //this is a hack to allow the ioc container to inject something, but then in the EndpointFilter we actually set the api service based on the request
    private class MiniLcmHolder
    {
        internal IMiniLcmApi MiniLcmApi
        {
            get => field ?? throw new InvalidOperationException("MiniLcmApi not set");
            set;
        } = null!;
    }

    public static IEndpointConventionBuilder MapMiniLcmRoutes(this IEndpointRouteBuilder app, [StringSyntax("route")] string prefix)
    {
        var api = app.MapGroup(prefix + "/{projectType}/{projectCode}")
            .WithOpenApi(operation =>
            {
                operation.Parameters.Add(new()
                {
                    Name = "projectType",
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema()
                    {
                        Enum =
                        [
                            new OpenApiString(ProjectDataFormat.FwData.ToString()),
                            new OpenApiString(ProjectDataFormat.Harmony.ToString())
                        ],
                        Type = "string"
                    },
                });
                operation.Parameters.Add(new()
                {
                    Name = "projectCode",
                    In = ParameterLocation.Path,
                    Required = true
                });
                return operation;
            })
            .AddEndpointFilter(async (context, next) =>
            {
                var miniLcmHolder = context.Arguments.OfType<MiniLcmHolder>().FirstOrDefault();
                if (miniLcmHolder is null) throw new InvalidOperationException("MiniLcmHolder not found in arguments");
                var typeString = context.HttpContext.Request.RouteValues.GetValueOrDefault("projectType")?.ToString();
                if (!Enum.TryParse<ProjectDataFormat>(typeString,
                        out var type))
                {
                    return Results.Problem($"Invalid project {typeString} type");
                }

                var projectCode = context.HttpContext.Request.RouteValues.GetValueOrDefault("projectCode")?.ToString();
                if (string.IsNullOrWhiteSpace(projectCode))
                {
                    return Results.Problem("Project code not found");
                }

                var projectProviders = context.HttpContext.RequestServices.GetServices<IProjectProvider>();
                var projectProvider = projectProviders.FirstOrDefault(p => p.DataFormat == type);
                if (projectProvider is null)
                {
                    throw new InvalidOperationException($"No project provider found for {type}");
                }

                var project = projectProvider.GetProject(projectCode);
                if (project is null)
                {
                    return Results.Problem($"Project {projectCode} not found");
                }

                var validationWrapperFactory = context.HttpContext.RequestServices
                    .GetRequiredService<MiniLcmApiValidationWrapperFactory>();

                miniLcmHolder.MiniLcmApi = validationWrapperFactory.Create(
                    await projectProvider.OpenProject(project, context.HttpContext.RequestServices)
                );
                return await next(context);
            });

        api.MapGet("/writingSystems", MiniLcm.GetWritingSystems);
        api.MapGet("/entries", MiniLcm.GetEntries);
        api.MapGet("/entries/window", MiniLcm.GetEntriesWindow);
        api.MapGet("/entries/{search}", MiniLcm.SearchEntries);
        api.MapGet("/entry/{id:Guid}", MiniLcm.GetEntry);
        api.MapGet("/entry/{id:Guid}/index", MiniLcm.GetEntryIndex);
        api.MapGet("/parts-of-speech", MiniLcm.GetPartsOfSpeech);
        api.MapGet("/semantic-domains", MiniLcm.GetSemanticDomains);
        api.MapGet("/publications", MiniLcm.GetPublications);
        api.MapPost("/entry", MiniLcm.PostEntry);
        api.MapDelete("/entry/{id:Guid}", MiniLcm.DeleteEntry);
        return api;
    }

    //swagger docs pickup their controller name from the type that the callback is defined in, that's why this type exists.
    private static class MiniLcm
    {
        public static Task<WritingSystems> GetWritingSystems([FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return api.GetWritingSystems();
        }

        public static IAsyncEnumerable<Entry> GetEntries([FromServices] MiniLcmHolder holder,
            [AsParameters] MiniLcmQueryOptions options)
        {
            var api = holder.MiniLcmApi;
            return api.GetEntries(options.ToQueryOptions());
        }

        public static IAsyncEnumerable<Entry> SearchEntries([FromServices] MiniLcmHolder holder,
            [FromRoute] string search,
            [AsParameters] MiniLcmQueryOptions options)
        {
            var api = holder.MiniLcmApi;
            return api.SearchEntries(search, options.ToQueryOptions());
        }

        public static Task<Entry?> GetEntry(Guid id, [FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return api.GetEntry(id);
        }

        public static Task<EntryWindowResponse> GetEntriesWindow(
            [FromQuery] int start,
            [FromQuery] int size,
            [AsParameters] MiniLcmQueryOptions options,
            [FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return api.GetEntriesWindow(start, size, null, options.ToQueryOptions());
        }

        public static Task<int> GetEntryIndex(
            Guid id,
            [AsParameters] MiniLcmQueryOptions options,
            [FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return api.GetEntryIndex(id, null, options.ToQueryOptions());
        }

        public static IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech([FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return api.GetPartsOfSpeech();
        }

        public static IAsyncEnumerable<SemanticDomain> GetSemanticDomains([FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return api.GetSemanticDomains();
        }

        public static IAsyncEnumerable<Publication> GetPublications([FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return api.GetPublications();
        }

        public static Task<Entry> PostEntry([FromBody] Entry entry, [FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return api.CreateEntry(entry);
        }

        public static Task DeleteEntry(Guid id, [FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return api.DeleteEntry(id);
        }
    }

    private class MiniLcmQueryOptions
    {
        public QueryOptions ToQueryOptions()
        {
            ExemplarOptions? exemplarOptions = string.IsNullOrEmpty(ExemplarValue) || ExemplarWritingSystem is null
                ? null
                : new(ExemplarValue, ExemplarWritingSystem);
            var sortField = SortField ?? SortOptions.Default.Field;
            return new QueryOptions(new SortOptions(sortField,
                    SortWritingSystem ?? SortOptions.Default.WritingSystem,
                    Ascending ?? SortOptions.Default.Ascending),
                exemplarOptions,
                Count ?? QueryOptions.Default.Count,
                Offset ?? QueryOptions.Default.Offset,
                string.IsNullOrEmpty(GridifyFilter) ? null : new EntryFilter {GridifyFilter = GridifyFilter});
        }

        public SortField? SortField { get; set; } = SortOptions.Default.Field;

        [DefaultValue(SortOptions.DefaultWritingSystem)]
        public string? SortWritingSystem { get; set; } = SortOptions.Default.WritingSystem;

        [FromQuery]
        [DefaultValue(true)]
        public bool? Ascending { get; set; }

        [FromQuery]
        public string? ExemplarValue { get; set; }

        public string? ExemplarWritingSystem { get; set; }

        [FromQuery]
        [DefaultValue(QueryOptions.DefaultCount)]
        public int? Count { get; set; }

        [FromQuery]
        public int? Offset { get; set; }
        [FromQuery]
        public string? GridifyFilter { get; set; }
    }
}
