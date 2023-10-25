using System.Text.Json.Serialization;
using LexBoxApi;
using LexBoxApi.Auth;
using LexBoxApi.ErrorHandling;
using LexBoxApi.Otel;
using LexBoxApi.Services;
using LexCore.Exceptions;
using LexData;
using LexSyncReverseProxy;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.OpenApi.Models;
using tusdotnet;

if (DbStartupService.IsMigrationRequest(args))
{
    await DbStartupService.RunMigrationRequest(args);
    return;
}

if (DevGqlSchemaWriterService.IsSchemaGenerationRequest(args))
{
    await DevGqlSchemaWriterService.GenerateGqlSchema(args);
    return;
}

var builder = WebApplication.CreateBuilder(args);
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
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LexBoxApi",
        Description = """
            This is the open api for LexBox, most of the api is in the [graphql endpoint](/api/graphql/ui).
            However there are some test users for login here, with the default password of `pass`:
            * admin@test.com (site admin)
            * manager@test.com (Sena 3 manager)
            * editor@test.com (Sena 3 editor)
            """,
    });
});
builder.Services.AddHealthChecks();
//in prod the exception handler middleware adds the exception feature, but in dev we need to do it manually
builder.Services.AddSingleton<IDeveloperPageExceptionFilter, AddExceptionFeatureDevExceptionFilter>();
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
    options.ResponseHeaders.Add("WWW-Authenticate");
    options.ResponseHeaders.Add("lexbox-version");
#if DEBUG
    options.RequestHeaders.Add("Cookie");
#endif
});

builder.Services.AddLexData(builder.Environment.IsDevelopment());
builder.Services.AddLexBoxApi(builder.Configuration, builder.Environment);

var app = builder.Build();
app.Logger.LogInformation("LexBox-api version: {version}", AppVersionService.Version);

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.All,
    AllowedHosts =
    {
        "localhost",
        "languagedepot.org",
        "*.languagedepot.org",
        "*.languageforge.org"
    }
});
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("lexbox-version", AppVersionService.Version);
    await next();
});
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();
app.UseHealthChecks("/api/healthz");
// Configure the HTTP request pipeline.
//for now allow this to run in prod, maybe later we want to disable it.
{
    app.UseHttpLogging();
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "api/swagger/{documentName}/swagger.{json|yaml}";
    });
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "api/swagger";
        options.ConfigObject.DisplayRequestDuration = true;
        options.EnableTryItOutByDefault();
    });
}
app.UseRouting();
app.UseResumableStatusHack();
app.UseAuthentication();
app.UseAuthorization();
app.MapBananaCakePop("/api/graphql/ui").AllowAnonymous();
if (app.Environment.IsDevelopment())
    //required for vite to generate types
    app.MapGraphQLSchema("/api/graphql/schema.graphql").AllowAnonymous();
app.MapGraphQLHttp("/api/graphql");
app.MapControllers();
app.MapTus("/api/tus-test",
        async context => await context.RequestServices.GetRequiredService<TusService>().GetTestConfig(context))
    .RequireAuthorization(new AdminRequiredAttribute());
app.MapTus($"/api/project/upload-zip/{{{ProxyConstants.HgProjectCodeRouteKey}}}",
        async context => await context.RequestServices.GetRequiredService<TusService>().GetResetZipUploadConfig())
    .RequireAuthorization(new AdminRequiredAttribute());
// /api routes should never make it to this point, they should be handled by the controllers, so return 404
app.Map("/api/{**catch-all}", () => Results.NotFound()).AllowAnonymous();
app.MapSyncProxy(AuthKernel.DefaultScheme);

await app.RunAsync();
