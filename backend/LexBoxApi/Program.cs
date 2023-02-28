using System.Text.Json.Serialization;
using HotChocolate.AspNetCore;
using LexBoxApi;
using LexData;
using LexSyncReverseProxy;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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