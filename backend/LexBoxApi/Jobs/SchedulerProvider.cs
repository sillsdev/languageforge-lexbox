using CrystalQuartz.Core.Contracts;
using CrystalQuartz.Core.SchedulerProviders;
using Quartz;

namespace LexBoxApi.Jobs;

public class SchedulerProvider(ISchedulerFactory schedulerFactory) : ISchedulerProvider
{
    public object CreateScheduler(ISchedulerEngine engine)
    {
        return schedulerFactory.GetScheduler();
    }
}
