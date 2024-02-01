using LexBoxApi.Services;
using LexCore.ServiceInterfaces;
using Quartz;

namespace LexBoxApi.Jobs;

public class CleanupResetBackupJob(IHgService hgService) : IJob
{
    public static JobKey Key { get; } = new("CleanupResetBackupJob");


    public async Task Execute(IJobExecutionContext context)
    {
        try
        {

        }
        catch (Exception e)
        {
            throw new JobExecutionException(e);
        }
    }
}
