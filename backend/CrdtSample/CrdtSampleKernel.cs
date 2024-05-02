using System.Diagnostics;
using CrdtSample.Changes;
using CrdtSample.Models;
using CrdtLib;
using CrdtLib.Changes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CrdtSample;

public static class CrdtSampleKernel
{
    public static IServiceCollection AddCrdtDataSample(this IServiceCollection services,
        string dbPath,
        bool enableProjectedTables = true)
    {
        services.AddCrdtData(
            builder =>
            {
                builder.UseSqlite($"Data Source={dbPath}");
                builder.EnableDetailedErrors();
                builder.EnableSensitiveDataLogging();
                #if DEBUG
                builder.LogTo(s => Debug.WriteLine(s));
                #endif
            },
            config =>
            {
                config.EnableProjectedTables = enableProjectedTables;
                config.ChangeTypeListBuilder
                    .Add<NewWordChange>()
                    .Add<NewDefinitionChange>()
                    .Add<NewExampleChange>()
                    .Add<EditExampleChange>()
                    .Add<SetWordTextChange>()
                    .Add<SetWordNoteChange>()
                    .Add<AddAntonymReferenceChange>()
                    .Add<SetOrderChange<Definition>>()
                    .Add<DeleteChange<Word>>()
                    .Add<DeleteChange<Definition>>()
                    .Add<DeleteChange<Example>>()
                    ;
                config.ObjectTypeListBuilder
                    .Add<Word>()
                    .Add<Definition>()
                    .Add<Example>();
            });
        return services;
    }
}