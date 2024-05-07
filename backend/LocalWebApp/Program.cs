using System.Text.RegularExpressions;
using LcmCrdt;
using LocalWebApp;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.FileProviders;
using MiniLcm;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment())
    builder.WebHost.UseUrls("http://127.0.0.1:0");

builder.Services.AddLocalAppServices();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR().AddJsonProtocol();

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
app.MapHub<LexboxApiHub>($"/api/hub/{{{LexboxApiHub.ProjectRouteKey}}}/lexbox");
app.MapGet("/api/projects", (ProjectsService projectService) => projectService.ListProjects());
Regex alphaNumericRegex = new Regex("^[a-zA-Z0-9]*$");
app.MapPost("/api/project",
    async (ProjectsService projectService, string name) =>
    {
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest("Project name is required");
        if (projectService.ProjectExists(name))
            return Results.BadRequest("Project already exists");
        if (!alphaNumericRegex.IsMatch(name))
            return Results.BadRequest("Project name must be alphanumeric");
        await projectService.CreateProject(name, afterCreate: async (provider, project) =>
        {
            var lexboxApi = provider.GetRequiredService<ILexboxApi>();
            await lexboxApi.CreateEntry(new()
            {
                Id = Guid.NewGuid(),
                LexemeForm = { Values = { { "en", "Kevin" } } },
                Note = { Values = { { "en", "this is a test note from Kevin" } } },
                CitationForm = { Values = { { "en", "Kevin" } } },
                LiteralMeaning = { Values = { { "en", "Kevin" } } },
                Senses =
                [
                    new()
                    {
                        Gloss = { Values = { { "en", "Kevin" } } },
                        Definition = { Values = { { "en", "Kevin" } } },
                        SemanticDomain = ["Person"],
                        ExampleSentences =
                        [
                            new() { Sentence = { Values = { { "en", "Kevin is a good guy" } } } }
                        ]
                    }
                ]
            });

            await lexboxApi.CreateWritingSystem(WritingSystemType.Vernacular,
                new()
                {
                    Id = "en",
                    Name = "English",
                    Abbreviation = "en",
                    Font = "Arial",
                    Exemplars = WritingSystem.LatinExemplars
                });

            await lexboxApi.CreateWritingSystem(WritingSystemType.Analysis,
                new()
                {
                    Id = "en",
                    Name = "English",
                    Abbreviation = "en",
                    Font = "Arial",
                    Exemplars = WritingSystem.LatinExemplars
                });
        });
        return TypedResults.Ok();
    });

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
