using Quartz;

namespace LexBoxApi.Jobs;

public abstract class LexJob: IJob
{
    async Task IJob.Execute(IJobExecutionContext context)
    {
        try
        {
            await ExecuteJob(context);
        }
        catch (Exception e)
        {
            throw new JobExecutionException(e);
        }
    }

    protected abstract Task ExecuteJob(IJobExecutionContext context);
}
