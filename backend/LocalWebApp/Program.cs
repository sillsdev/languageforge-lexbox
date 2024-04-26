using System.Text.Json.Serialization.Metadata;
using CrdtLib;
using LcmCrdt;
using LocalWebApp;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment())
    builder.WebHost.UseUrls("http://127.0.0.1:0");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR().AddJsonProtocol();
builder.Services.AddLcmCrdtClient("tmp.sqlite", builder.Services.BuildServiceProvider().GetService<ILoggerFactory>());
builder.Services.AddOptions<JsonOptions>().PostConfigure<IOptions<CrdtConfig>>((jsonOptions, crdtConfig) =>
{
    jsonOptions.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
    {
        Modifiers = { crdtConfig.Value.MakeJsonTypeModifier() }
    };
});
builder.Services.AddOptions<JsonHubProtocolOptions>().PostConfigure<IOptions<CrdtConfig>>((jsonOptions, crdtConfig) =>
{
    jsonOptions.PayloadSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
    {
        Modifiers = { crdtConfig.Value.MakeJsonTypeModifier() }
    };
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var sharedOptions = new SharedOptions() { FileProvider = new ManifestEmbeddedFileProvider(typeof(Program).Assembly) };
app.UseDefaultFiles(new DefaultFilesOptions(sharedOptions));
app.UseStaticFiles(new StaticFileOptions(sharedOptions));
app.MapHub<LexboxApiHub>("/api/hub/project");

await using (app)
{
    await app.StartAsync();

    if (!app.Environment.IsDevelopment())
    {
        var url = app.Urls.First();
        LocalAppLauncher.LaunchBrowser(url);
    }

    await app.WaitForShutdownAsync();
}
