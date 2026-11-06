using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;
using MiniLcm.Models;

namespace MiniLcmWeb;

public static class MiniLcmRoutes
{
    public static IServiceCollection AddMiniLcmRouteServices(this IServiceCollection services)
    {
        services.AddTransient<MiniLcmHolder>();
        services.AddMiniLcmJsonOptions();
        return services;
    }

    /// <summary>
    /// Hides internal MiniLcm properties from API callers, in both directions. Also used by hosts that only
    /// describe these routes in OpenAPI, so the schema matches what the routes accept and return.
    /// </summary>
    public static IServiceCollection AddMiniLcmJsonOptions(this IServiceCollection services)
    {
        services.AddOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>().PostConfigure(jsonOptions =>
        {
            var options = jsonOptions.SerializerOptions;
            options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver()).AddMiniLcmApiModifiers();
        });
        return services;
    }

    //this is a hack to allow the ioc container to inject something, but then in the EndpointFilter we actually set the api service based on the request
    private class MiniLcmHolder
    {
        internal IMiniLcmApi MiniLcmApi
        {
            get => field ?? throw new InvalidOperationException("MiniLcmApi not set");
            set;
        } = null;
    }

    /// <summary>
    /// Maps the MiniLcm routes under <paramref name="prefix"/>, which carries whatever route values
    /// <paramref name="resolver"/> needs to find the project.
    /// </summary>
    public static RouteGroupBuilder MapMiniLcmRoutes(this IEndpointRouteBuilder app,
        [StringSyntax("route")] string prefix,
        MiniLcmApiResolver resolver)
    {
        var api = app.MapGroup(prefix)
            .AddEndpointFilter(async (context, next) =>
            {
                var miniLcmHolder = context.Arguments.OfType<MiniLcmHolder>().FirstOrDefault();
                if (miniLcmHolder is null) throw new InvalidOperationException("MiniLcmHolder not found in arguments");
                var resolution = await resolver(context.HttpContext);
                if (resolution.Error is not null) return resolution.Error;
                miniLcmHolder.MiniLcmApi = resolution.Api ?? throw new InvalidOperationException("MiniLcm api resolver returned neither an api nor an error");
                return await next(context);
            })
            .WithQueryParameterDocs();

        api.MapGet("/writing-systems", MiniLcm.GetWritingSystems).WithDoc(MiniLcmRouteDocs.GetWritingSystems);
        api.MapGet("/entries", MiniLcm.GetEntries).WithDoc(MiniLcmRouteDocs.GetEntries);
        api.MapGet("/entries/count", MiniLcm.CountEntries).WithDoc(MiniLcmRouteDocs.CountEntries);
        api.MapGet("/entry/{id:Guid}", MiniLcm.GetEntry).WithDoc(MiniLcmRouteDocs.GetEntry);
        api.MapGet("/entry/{id:Guid}/index", MiniLcm.GetEntryIndex).WithDoc(MiniLcmRouteDocs.GetEntryIndex);
        api.MapGet("/sense/{id:Guid}", MiniLcm.GetSense).WithDoc(MiniLcmRouteDocs.GetSense);
        api.MapGet("/parts-of-speech", MiniLcm.GetPartsOfSpeech).WithDoc(MiniLcmRouteDocs.GetPartsOfSpeech);
        api.MapGet("/semantic-domains", MiniLcm.GetSemanticDomains).WithDoc(MiniLcmRouteDocs.GetSemanticDomains);
        api.MapGet("/publications", MiniLcm.GetPublications).WithDoc(MiniLcmRouteDocs.GetPublications);
        api.MapGet("/complex-form-types", MiniLcm.GetComplexFormTypes).WithDoc(MiniLcmRouteDocs.GetComplexFormTypes);
        api.MapGet("/morph-types", MiniLcm.GetMorphTypes).WithDoc(MiniLcmRouteDocs.GetMorphTypes);
        api.MapPost("/entry", MiniLcm.PostEntry).WithDoc(MiniLcmRouteDocs.CreateEntry);
        api.MapPut("/entry/{id:Guid}", MiniLcm.PutEntry).ProducesProblem(StatusCodes.Status400BadRequest).WithDoc(MiniLcmRouteDocs.UpdateEntry);
        api.MapDelete("/entry/{id:Guid}", MiniLcm.DeleteEntry).WithDoc(MiniLcmRouteDocs.DeleteEntry);
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
            [FromQuery] string? search,
            [AsParameters] MiniLcmQueryOptions options)
        {
            var api = holder.MiniLcmApi;
            return string.IsNullOrEmpty(search)
                ? api.GetEntries(options.ToQueryOptions())
                : api.SearchEntries(search, options.ToQueryOptions());
        }

        public static Task<int> CountEntries([FromServices] MiniLcmHolder holder,
            [FromQuery] string? search,
            [FromQuery] string? gridifyFilter)
        {
            var api = holder.MiniLcmApi;
            return api.CountEntries(string.IsNullOrEmpty(search) ? null : search,
                new FilterQueryOptions(Filter: MiniLcmQueryOptions.ToEntryFilter(gridifyFilter)));
        }

        public static async Task<Results<Ok<Entry>, NotFound>> GetEntry(Guid id, [FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return await api.GetEntry(id) is { } entry ? TypedResults.Ok(entry) : TypedResults.NotFound();
        }

        public static Task<int> GetEntryIndex(
            Guid id,
            [FromQuery] string? search,
            [AsParameters] MiniLcmQueryOptions options,
            [FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return api.GetEntryIndex(id, search, options.ToIndexQueryOptions());
        }

        public static async Task<Results<Ok<Sense>, NotFound>> GetSense(Guid id, [FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return await api.GetSense(id) is { } sense ? TypedResults.Ok(sense) : TypedResults.NotFound();
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

        public static IAsyncEnumerable<ComplexFormType> GetComplexFormTypes([FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return api.GetComplexFormTypes();
        }

        public static IAsyncEnumerable<MorphType> GetMorphTypes([FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return api.GetMorphTypes();
        }

        public static async Task<Created<Entry>> PostEntry([FromBody] Entry entry,
            [FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            var created = await api.CreateEntry(entry, CreateEntryOptions.WithMainPublication);
            //relative to the request path, so it holds behind any prefix or proxy
            return TypedResults.Created($"entry/{created.Id}", created);
        }

        public static async Task<Results<Ok<Entry>, ProblemHttpResult>> PutEntry(Guid id,
            [FromBody] UpdateEntryRequest request,
            [FromServices] MiniLcmHolder holder)
        {
            //null when missing from the body, despite the non-nullable types
            if (request.Before is null || request.After is null)
            {
                return TypedResults.Problem("Both 'before' and 'after' entries are required",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (request.Before.Id != id || request.After.Id != id)
            {
                return TypedResults.Problem($"Entry ids in the body must match the route id '{id}'",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var api = holder.MiniLcmApi;
            return TypedResults.Ok(await api.UpdateEntry(request.Before, request.After));
        }

        public static Task DeleteEntry(Guid id, [FromServices] MiniLcmHolder holder)
        {
            var api = holder.MiniLcmApi;
            return api.DeleteEntry(id);
        }
    }
}

/// <summary>
/// Body of <c>PUT entry/{id}</c>: the entry as the caller last read it, and as it should be now.
/// </summary>
public record UpdateEntryRequest(Entry Before, Entry After);
