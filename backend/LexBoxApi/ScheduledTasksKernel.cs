using CrystalQuartz.Application;
using CrystalQuartz.Application.Startup;
using CrystalQuartz.AspNetCore;
using LexBoxApi.Jobs;
using Quartz;
using Quartz.AspNetCore;

namespace LexBoxApi;

public static class ScheduledTasksKernel
{
    public static void AddScheduledTasks(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            q.AddJob<CleanupResetBackupJob>(CleanupResetBackupJob.Key);
            q.AddTrigger(opts => opts.ForJob(CleanupResetBackupJob.Key)
                .WithIdentity("WeeklyCleanupTrigger")
                //every sunday at 2am
                .WithCronSchedule("0 0 2 ? * 1", builder => builder.WithMisfireHandlingInstructionFireAndProceed()));
        });
        services.AddQuartzServer();
    }

    public static IEndpointConventionBuilder MapQuartzUI(this IEndpointRouteBuilder endpoints)
    {
        var options = new CrystalQuartzOptions().ToRuntimeOptions(SchedulerEngineProviders.SchedulerEngineResolvers,
            ".NET Standard 2.1");
        //using this instead of the default UseCrystalQuartz so that we can apply auth to it.
        return endpoints.Map("/api/quartz",
            async context =>
            {
                await new CrystalQuartzPanelMiddleware(null,
                    new SchedulerProvider(context.RequestServices.GetRequiredService<ISchedulerFactory>()),
                    options
                ).Invoke(context);
            });
    }
}
