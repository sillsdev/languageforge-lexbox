using System.Diagnostics;
using System.Text.Json.Serialization;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Requirements;
using LexBoxApi.Services;
using LexCore.Config;
using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniLcm.Models;
using MiniLcmWeb;
using OpenIddict.Abstractions;
using SIL.Harmony.Core;
using Yarp.ReverseProxy.Forwarder;

namespace LexBoxApi.Proxies;

/// <summary>
/// The lexicon API: MiniLcm routes over a project's headless harmony project, forwarded to FwHeadless, which serves them
/// at the same paths. Lexbox owns auth: scopes and project roles are checked here, and the caller's identity reaches
/// FwHeadless as the X-Lexbox-* headers, which FwHeadless stamps on the commits a write makes.
/// </summary>
public static class LexiconProxy
{
    public const string ListPolicy = "LexiconList";
    public const string ReadPolicy = "LexiconRead";
    public const string WritePolicy = "LexiconWrite";
    // mirrored by FwHeadless.Routes.LexiconRoutes
    public const string UserIdHeader = "X-Lexbox-User-Id";
    public const string UserNameHeader = "X-Lexbox-User-Name";
    public const string ClientIdHeader = "X-Lexbox-Client-Id";
    /// <summary>The client id sent for sessions that don't come from an OAuth client (browser cookie, Lexbox JWT).</summary>
    public const string NoOAuthClientId = "lexbox";

    private const string Experimental =
        "Experimental: the lexicon API may change without notice. Errors use `application/problem+json`; " +
        "a 409 with type `/problems/project-not-ready` means the project has not been synced to FieldWorks Lite on Lexbox yet, " +
        "and a 409 with type `/problems/sync-in-progress` means a write was refused while the project syncs with FieldWorks.";

    public static void AddLexiconProxy(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, LexiconProjectRequirementHandler>();
        // the forwarder itself is registered by AddFileUploadProxy
    }

    public static IEndpointConventionBuilder MapLexiconProxy(this IEndpointRouteBuilder app)
    {
        var lexicon = app.MapGroup("/api/lexicon")
            .WithGroupName(LexboxOpenApi.OpenApiPublicDocumentName)
            .WithTags("Lexicon");

        lexicon.MapGet("/projects", ListProjects)
            .RequireAuthorization(ListPolicy)
            .WithSummary("List projects")
            .WithDescription("The Harmony projects you are a member of that the lexicon API can serve, with your role in each. " +
                             "Projects that have not been synced to FieldWorks Lite on Lexbox yet are left out.\n\n" + Experimental);

        var project = lexicon.MapGroup("/project/{projectId:guid}")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Parameters are bound only so the OpenAPI document describes them; FwHeadless reads the forwarded request.
        var read = project.MapGroup("").RequireAuthorization(ReadPolicy).WithQueryParameterDocs();
        read.MapGet("/writing-systems", ForwardProject).Produces<WritingSystems>().WithDoc(MiniLcmRouteDocs.GetWritingSystems, Experimental);
        read.MapGet("/entries",
                (Guid projectId, [FromQuery] string? search, [AsParameters] MiniLcmQueryOptions options, HttpContext context) => Forward(context))
            .Produces<Entry[]>().WithDoc(MiniLcmRouteDocs.GetEntries, Experimental);
        read.MapGet("/entries/count",
                (Guid projectId, [FromQuery] string? search, [FromQuery] string? gridifyFilter, HttpContext context) => Forward(context))
            .Produces<int>().WithDoc(MiniLcmRouteDocs.CountEntries, Experimental);
        read.MapGet("/entry/{id:guid}", ForwardItem).Produces<Entry>().Produces(StatusCodes.Status404NotFound).WithDoc(MiniLcmRouteDocs.GetEntry, Experimental);
        read.MapGet("/entry/{id:guid}/index",
                (Guid projectId, Guid id, [FromQuery] string? search, [AsParameters] MiniLcmQueryOptions options, HttpContext context) => Forward(context))
            .Produces<int>().WithDoc(MiniLcmRouteDocs.GetEntryIndex, Experimental);
        read.MapGet("/sense/{id:guid}", ForwardItem).Produces<Sense>().Produces(StatusCodes.Status404NotFound).WithDoc(MiniLcmRouteDocs.GetSense, Experimental);
        read.MapGet("/parts-of-speech", ForwardProject).Produces<PartOfSpeech[]>().WithDoc(MiniLcmRouteDocs.GetPartsOfSpeech, Experimental);
        read.MapGet("/semantic-domains", ForwardProject).Produces<SemanticDomain[]>().WithDoc(MiniLcmRouteDocs.GetSemanticDomains, Experimental);
        read.MapGet("/publications", ForwardProject).Produces<Publication[]>().WithDoc(MiniLcmRouteDocs.GetPublications, Experimental);
        read.MapGet("/complex-form-types", ForwardProject).Produces<ComplexFormType[]>().WithDoc(MiniLcmRouteDocs.GetComplexFormTypes, Experimental);
        read.MapGet("/morph-types", ForwardProject).Produces<MorphType[]>().WithDoc(MiniLcmRouteDocs.GetMorphTypes, Experimental);

        var write = project.MapGroup("").RequireAuthorization(WritePolicy);
        write.MapPost("/entry", ForwardProject).Accepts<Entry>("application/json").Produces<Entry>(StatusCodes.Status201Created).WithDoc(MiniLcmRouteDocs.CreateEntry, Experimental);
        write.MapPut("/entry/{id:guid}", ForwardItem).Accepts<UpdateEntryRequest>("application/json")
            .Produces<Entry>().ProducesProblem(StatusCodes.Status400BadRequest).WithDoc(MiniLcmRouteDocs.UpdateEntry, Experimental);
        write.MapDelete("/entry/{id:guid}", ForwardItem).Produces(StatusCodes.Status200OK).WithDoc(MiniLcmRouteDocs.DeleteEntry, Experimental);
        return lexicon;
    }

    public record LexiconProject(Guid Id, string Code, string Name,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] ProjectRole Role);

    private static async Task<Ok<LexiconProject[]>> ListProjects(LoggedInContext loggedInContext,
        ProjectService projectService,
        LexBoxDbContext dbContext,
        FwHeadlessClient fwHeadlessClient)
    {
        var userId = loggedInContext.User.Id;
        var candidates = await projectService.UserProjects(userId)
            .Where(p => p.Type == ProjectType.FLEx && dbContext.Set<ServerCommit>().Any(c => c.ProjectId == p.Id))
            .Select(p => new LexiconProject(p.Id,
                p.Code,
                p.Name,
                p.Users.Where(u => u.UserId == userId).Select(m => m.Role).First()))
            .ToArrayAsync();
        if (candidates.Length == 0) return TypedResults.Ok(candidates);
        var ready = (await fwHeadlessClient.ReadyLexiconProjects(candidates.Select(p => p.Id).ToArray())).ToHashSet();
        return TypedResults.Ok(candidates.Where(p => ready.Contains(p.Id)).ToArray());
    }

    private static readonly Func<Guid, HttpContext, Task> ForwardProject = (projectId, context) => Forward(context);
    private static readonly Func<Guid, Guid, HttpContext, Task> ForwardItem = (projectId, id, context) => Forward(context);

    private static async Task Forward(HttpContext context)
    {
        Activity.Current?.AddTag("app.lexicon_api", true);
        var httpClient = context.RequestServices.GetRequiredService<HttpMessageInvoker>();
        var forwarder = context.RequestServices.GetRequiredService<IHttpForwarder>();
        var destinationPrefix = context.RequestServices.GetRequiredService<IOptions<MediaFileConfig>>().Value.FwHeadlessUrl;
        var user = context.RequestServices.GetRequiredService<LoggedInContext>().User;
        var clientId = context.User.FindFirst(OpenIddictConstants.Claims.ClientId)?.Value;
        var transformer = new IdentityHeadersTransformer(user.Id.ToString(),
            user.Name,
            string.IsNullOrEmpty(clientId) ? NoOAuthClientId : clientId);
        await forwarder.SendAsync(context, destinationPrefix, httpClient, ForwarderRequestConfig.Empty, transformer);
    }

    private sealed class IdentityHeadersTransformer(string userId, string userName, string clientId) : HttpTransformer
    {
        public override async ValueTask TransformRequestAsync(HttpContext httpContext,
            HttpRequestMessage proxyRequest,
            string destinationPrefix,
            CancellationToken cancellationToken)
        {
            await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);
            // FwHeadless needs neither the caller's credentials nor any identity headers the caller made up
            proxyRequest.Headers.Remove("Authorization");
            proxyRequest.Headers.Remove("Cookie");
            proxyRequest.Headers.Remove(UserIdHeader);
            proxyRequest.Headers.Remove(UserNameHeader);
            proxyRequest.Headers.Remove(ClientIdHeader);
            proxyRequest.Headers.Add(UserIdHeader, userId);
            // display names aren't limited to ASCII, which headers are
            proxyRequest.Headers.Add(UserNameHeader, Uri.EscapeDataString(userName));
            proxyRequest.Headers.Add(ClientIdHeader, clientId);
        }
    }
}
