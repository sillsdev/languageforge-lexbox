using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL;
using CrystalQuartz.Application;
using CrystalQuartz.Application.Startup;
using CrystalQuartz.AspNetCore;
using LexBoxApi.Jobs;
using LexData;
using LexData.Configuration;
using Quartz;
using Quartz.AspNetCore;
using Quartz.Impl.AdoJobStore;

namespace LexBoxApi;

public static class ScheduledTasksKernel
{
    public static void AddScheduledTasks(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureDbModel(builder =>
        {
            builder.AddQuartz(b => b.UsePostgreSql());
        });
        services.AddQuartz(q =>
        {
            q.UsePersistentStore(options =>
            {
                options.UseProperties = true;
                options.UseSystemTextJsonSerializer();
                var connectionString = configuration.GetSection(nameof(DbConfig))
                    .GetValue<string>(nameof(DbConfig.LexBoxConnectionString));
                ArgumentNullException.ThrowIfNull(connectionString);
                options.UsePostgres(dbOptions =>
                {
                    dbOptions.ConnectionString = connectionString;
                    dbOptions.TablePrefix = "quartz." + AdoConstants.DefaultTablePrefix;
                });
            });

            //Setup jobs
            q.AddJob<CleanupResetBackupJob>(CleanupResetBackupJob.Key);
            q.AddJob<UpdateProjectMetadataJob>(UpdateProjectMetadataJob.Key, j => j.StoreDurably());
            q.AddJob<RetryEmailJob>(RetryEmailJob.Key, j => j.StoreDurably());
            q.AddTrigger(opts => opts.ForJob(CleanupResetBackupJob.Key)
                .WithIdentity("WeeklyCleanupTrigger")
                //every sunday at 2am
                .WithCronSchedule("0 0 2 ? * 1", builder => builder.WithMisfireHandlingInstructionFireAndProceed()));
        });
        services.AddQuartzServer();
    }

    public static IEndpointConventionBuilder MapQuartzUI(this IEndpointRouteBuilder endpoints, string path)
    {
        var options = new CrystalQuartzOptions().ToRuntimeOptions(SchedulerEngineProviders.SchedulerEngineResolvers,
            ".NET Standard 2.1");
        //using this instead of the default UseCrystalQuartz so that we can apply auth to it.
        return endpoints.Map(path,
            async context =>
            {
                await new CrystalQuartzPanelMiddleware(null,
                    new SchedulerProvider(context.RequestServices.GetRequiredService<ISchedulerFactory>()),
                    options
                ).Invoke(context);
            });
    }
}
