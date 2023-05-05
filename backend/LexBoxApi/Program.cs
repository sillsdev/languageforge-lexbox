using System.Text.Json.Serialization;
using LexBoxApi;
using LexBoxApi.Auth;
using LexBoxApi.Otel;
using LexBoxApi.Services;
using LexData;
using LexSyncReverseProxy;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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
        // Description = " Hello ",
        Description = """
            This is the open api for LexBox, most of api is in the [graphql endpoint](/api/graphql/ui).
            However there are some test users for login here, with the default password of `pass`:
            * KindLion@test.com (site admin)
            * InnocentMoth@test.com (Sena 3 manager)
            * PlayfulFish@test.com (Sena 3 editor)
            """,
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                            HttpLoggingFields.ResponsePropertiesAndHeaders;
});

builder.Services.AddLexData();
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

// Configure the HTTP request pipeline.
//for now allow this to run in prod, maybe later we want to disable it.
// if (app.Environment.IsDevelopment())
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
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/api/healthz").AllowAnonymous();
app.MapBananaCakePop("/api/graphql/ui").AllowAnonymous();
if (app.Environment.IsDevelopment())
    //required for vite to generate types
    app.MapGraphQLSchema("/api/graphql/schema.graphql").AllowAnonymous();
app.MapGraphQLHttp("/api/graphql");
app.MapControllers();

//disabled in dev because it'll make it hard to trace routing errors
if (!app.Environment.IsDevelopment())
    app.MapSyncProxy(AuthKernel.DefaultScheme);

app.Run();