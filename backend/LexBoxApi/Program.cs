using System.Text.Json.Serialization;
using LexBoxApi;
using LexData;
using LexSyncReverseProxy;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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
            * KindLion@test.com
            * InnocentMoth@test.com
            * PlayfulFish@test.com
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();
    app.UseSwagger();
    app.UseSwaggerUI(options => options.EnableTryItOutByDefault());
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/api/healthz").AllowAnonymous();
app.MapBananaCakePop("/api/graphql/ui").AllowAnonymous();
app.MapGraphQLHttp("/api/graphql");
app.MapControllers();

//disabled in dev because it'll make it hard to trace routing errors
if (app.Environment.IsProduction())
    app.MapSyncProxy();

app.Run();