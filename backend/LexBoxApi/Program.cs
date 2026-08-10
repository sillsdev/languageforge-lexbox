using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.AspNetCore;
using LexBoxApi;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.ErrorHandling;
using LexBoxApi.Hub;
using LexBoxApi.Otel;
using LexBoxApi.Proxies;
using LexBoxApi.Services;
using LexCore.Auth;
using LexCore.Exceptions;
using LexData;
using LexSyncReverseProxy;
using LexSyncReverseProxy.Auth;
using LfClassicData;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using tusdotnet;

if (MigrationKernel.IsMigrationRequest(args))
{
    await MigrationKernel.RunMigrationRequest(args);
    return;
}

if (DevGqlSchemaWriterService.IsSchemaGenerationRequest(args))
{
    await DevGqlSchemaWriterService.GenerateGqlSchema(args);
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Logging.Configure(options => options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId);
builder.Host.UseConsoleLifetime();
builder.WebHost.UseKestrel(o =>
{
    //allow large pushes from hg, can't scope this only to hg requests as this setting is still respected in some cases
    o.Limits.MaxRequestBodySize = null;
});
// Add services to the container.

builder.Services.AddOpenTelemetryInstrumentation(builder.Configuration);

builder.Services.AddControllers(options =>
{
    options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
}).AddControllersAsServices();
builder.Services.AddSignalR();
builder.Services.AddSingleton(services =>
    services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions);
builder.Services.AddLexboxOpenApi();

#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018
builder.Services.AddHealthChecks();
//in prod the exception handler middleware adds the exception feature, but in dev we need to do it manually
builder.Services.AddSingleton<IDeveloperPageExceptionFilter, AddExceptionFeatureDevExceptionFilter>();
builder.Services.AddExceptionHandler((options) =>
{
    options.StatusCodeSelector = exception =>
    {
        if (exception is UnauthorizedAccessException)
            return StatusCodes.Status401Unauthorized;
        return StatusCodes.Status500InternalServerError;
    };
});
builder.Services.AddProblemDetails(o =>
{
    o.CustomizeProblemDetails = context =>
    {
        var exceptionHandlerFeature = context.HttpContext.Features.Get<IExceptionHandlerFeature>();
        if (exceptionHandlerFeature?.Error is not IExceptionWithCode exceptionWithCode) return;
        context.ProblemDetails.Extensions["app-error-code"] = exceptionWithCode.Code;
    };
});
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                            HttpLoggingFields.ResponsePropertiesAndHeaders;
});
builder.Services.AddOptions<HttpLoggingOptions>()
    .PostConfigure((HttpLoggingOptions options, IConfiguration configuration) =>
    {
        foreach (var requestHeader in configuration.GetSection("HttpLoggingOptions:AdditionalRequestHeaders").GetChildren())
        {
            options.RequestHeaders.Add(requestHeader.Value!);
        }
        foreach (var requestHeader in configuration.GetSection("HttpLoggingOptions:AdditionalResponseHeaders").GetChildren())
        {
            options.ResponseHeaders.Add(requestHeader.Value!);
        }
    });

builder.Services.AddLexData(builder.Environment.IsDevelopment());
builder.Services.AddLexBoxApi(builder.Configuration, builder.Environment);
builder.Services.AddLanguageForgeClassicMiniLcm();
builder.Services.AddOptions<ForwardedHeadersOptions>()
    .BindConfiguration("ForwardedHeadersOptions")
    .PostConfigure((ForwardedHeadersOptions options, IConfiguration configuration) =>
    {
        //workaround issue that binding won't configure these properties
        foreach (var knownProxy in configuration.GetSection("ForwardedHeadersOptions:KnownProxies").GetChildren())
        {
            options.KnownProxies.Add(IPAddress.Parse(knownProxy.Value!));
        }

        foreach (var knownNetwork in configuration.GetSection("ForwardedHeadersOptions:KnownNetworks").GetChildren())
        {
            options.KnownIPNetworks.Add(IPNetwork.Parse(knownNetwork.Value!));
        }
    });

var app = builder.Build();
app.Logger.LogInformation("LexBox-api version: {version}", AppVersionService.Version);

app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    context.Response.Headers["lexbox-version"] = AppVersionService.Version;
    await next();
});
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();
app.UseHealthChecks("/api/healthz");
// Configure the HTTP request pipeline.
app.UseHttpLogging();
app.UseRouting();
app.UseResumableStatusHack();
app.UseAuthentication();
app.UseAuthorization();
app.MapSecurityTxt();
app.MapNitroApp("/api/graphql/ui").WithOptions(new (){ServeMode = GraphQLToolServeMode.Embedded}).AllowAnonymous();
app.MapGraphQLSchema("/api/graphql/schema.graphql").AllowAnonymous();
app.MapGraphQLHttp("/api/graphql");
app.MapLexboxOpenApi().AllowAnonymous();

app.MapQuartzUI("/api/quartz").RequireAuthorization(new AdminRequiredAttribute());
app.MapControllers();
app.MapLfClassicApi().WithGroupName(LexboxOpenApi.OpenApiPublicDocumentName)
    .RequireAuthorization(policyBuilder => policyBuilder.RequireAuthenticatedUser().AddRequirements(new UserHasAccessToProjectRequirement()));
app.MapTus("/api/tus-test",
        async context => await context.RequestServices.GetRequiredService<TusService>().GetTestConfig(context))
    .RequireAuthorization(new AdminRequiredAttribute());
app.MapTus($"/api/project/upload-zip/{{{ProxyConstants.HgProjectCodeRouteKey}}}",
        async context => await context.RequestServices.GetRequiredService<TusService>().GetResetZipUploadConfig())
    .RequireAuthorization(new AdminRequiredAttribute());
app.MapHub<CrdtProjectChangeHub>("/api/hub/crdt/project-changes")
    .RequireAuthorization(new RequireScopeAttribute(LexboxAuthScope.LexboxApi, LexboxAuthScope.SendAndReceive));
// /api routes should never make it to this point, they should be handled by the controllers, so return 404
app.Map("/api/{**catch-all}", () => Results.NotFound()).AllowAnonymous();

//should normally be handled by svelte, but if it does reach this we need to return a 401, otherwise we'll get stuck in a redirect loop
app.Map("/login", Results.Unauthorized).AllowAnonymous();

app.MapFileUploadProxy();
app.MapSyncProxy(AuthKernel.DefaultScheme);

await app.RunAsync();
