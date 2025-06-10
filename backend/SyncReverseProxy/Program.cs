using LexCore.ServiceInterfaces;
using LexSyncReverseProxy;
using LexSyncReverseProxy.Config;
using LexSyncReverseProxy.Otel;
using LexSyncReverseProxy.Services;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOptions<LexBoxApiConfig>()
    .BindConfiguration("LexBoxApi")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHttpClient();
builder.Services.AddSyncProxy();
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                             HttpLoggingFields.ResponsePropertiesAndHeaders |
                             HttpLoggingFields.RequestQuery;
    options.RequestHeaders.Add(HeaderNames.Authorization);
    options.RequestHeaders.Add("x-hgarg-1");
    options.ResponseHeaders.Add(HeaderNames.WWWAuthenticate);
    options.ResponseHeaders.Add("X-HgR-Version");
    options.ResponseHeaders.Add("X-HgR-Status");
});
builder.Services.AddOpenTelemetryInstrumentation();

var app = builder.Build();

app.UseHttpLogging();

// app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapSyncProxy();
app.Run();
