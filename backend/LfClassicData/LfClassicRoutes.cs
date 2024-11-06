using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MiniLcm;
using MiniLcm.Models;

namespace LfClassicData;

public static class LfClassicRoutes
{
    //must match the route key in ProxyConstants
    public const string ProjectCodeRouteKey = "project-code";
    public static IEndpointConventionBuilder MapLfClassicApi(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/lfclassic/{project-code}");
        group.MapGet("/writingSystems", MiniLcm.GetWritingSystems);
        group.MapGet("/entries", MiniLcm.GetEntries);
        group.MapGet("/entries/{search}", MiniLcm.SearchEntries);
        group.MapGet("/entry/{id:Guid}", MiniLcm.GetEntry);
        group.MapGet("/parts-of-speech", MiniLcm.GetPartsOfSpeech);
        group.MapGet("/semantic-domains", MiniLcm.GetSemanticDomains);
        return group;
    }

    //swagger docs pickup their controller name from the type that the callback is defined in, that's why this type exists.
    private static class MiniLcm
    {
        public static Task<WritingSystems> GetWritingSystems([FromRoute(Name = ProjectCodeRouteKey)] string projectCode,
            [FromServices] LfClassicLexboxApiProvider provider)
        {
            var api = provider.GetProjectApi(projectCode);
            return api.GetWritingSystems();
        }

        public static IAsyncEnumerable<Entry> GetEntries([FromRoute(Name = ProjectCodeRouteKey)] string projectCode,
            [FromServices] LfClassicLexboxApiProvider provider,
            [AsParameters] ClassicQueryOptions options)
        {
            var api = provider.GetProjectApi(projectCode);
            return api.GetEntries(options.ToQueryOptions());
        }

        public static IAsyncEnumerable<Entry> SearchEntries([FromRoute(Name = ProjectCodeRouteKey)] string projectCode,
            [FromServices] LfClassicLexboxApiProvider provider,
            [FromRoute] string search,
            [AsParameters] ClassicQueryOptions options)
        {
            var api = provider.GetProjectApi(projectCode);
            return api.SearchEntries(search, options.ToQueryOptions());
        }

        public static Task<Entry?> GetEntry([FromRoute(Name = ProjectCodeRouteKey)] string projectCode,
            Guid id,
            [FromServices] LfClassicLexboxApiProvider provider)
        {
            var api = provider.GetProjectApi(projectCode);
            return api.GetEntry(id);
        }

        public static IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech([FromRoute(Name = ProjectCodeRouteKey)] string projectCode,
            [FromServices] LfClassicLexboxApiProvider provider)
        {
            var api = provider.GetProjectApi(projectCode);
            return api.GetPartsOfSpeech();
        }

        public static IAsyncEnumerable<SemanticDomain> GetSemanticDomains([FromRoute(Name = ProjectCodeRouteKey)] string projectCode,
            [FromServices] LfClassicLexboxApiProvider provider)
        {
            var api = provider.GetProjectApi(projectCode);
            return api.GetSemanticDomains();
        }
    }

    private class ClassicQueryOptions
    {
        public QueryOptions ToQueryOptions()
        {
            ExemplarOptions? exemplarOptions = string.IsNullOrEmpty(ExemplarValue) || ExemplarWritingSystem is null
                ? null
                : new(ExemplarValue, ExemplarWritingSystem.Value);
            var sortField = Enum.TryParse<SortField>(SortField, true, out var field)
                ? field
                : SortOptions.Default.Field;
            return new QueryOptions(new SortOptions(sortField,
                    SortWritingSystem ?? SortOptions.Default.WritingSystem,
                    Ascending ?? SortOptions.Default.Ascending),
                exemplarOptions,
                Count ?? QueryOptions.Default.Count,
                Offset ?? QueryOptions.Default.Offset);
        }

        public string? SortField { get; set; }

        public WritingSystemId? SortWritingSystem { get; set; }

        [FromQuery]
        public bool? Ascending { get; set; }

        [FromQuery]
        public string? ExemplarValue { get; set; }

        public WritingSystemId? ExemplarWritingSystem { get; set; }

        [FromQuery]
        public int? Count { get; set; }

        [FromQuery]
        public int? Offset { get; set; }
    }
}
