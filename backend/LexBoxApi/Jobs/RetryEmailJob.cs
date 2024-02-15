using LexBoxApi.Services;
using MimeKit;
using Quartz;

namespace LexBoxApi.Jobs;

public class RetryEmailJob(EmailService emailService) : LexJob
{
    public static async Task Queue(ISchedulerFactory schedulerFactory,
        MimeMessage email,
        int retryCount = 3,
        int retryWaitSeconds = 5 * 60,
        CancellationToken cancellationToken = default)
    {
        var memory = new MemoryStream();
        await email.WriteToAsync(memory, cancellationToken);
        await QueueJob(schedulerFactory,
            Key,
            new JobDataMap {
                { nameof(SerializedEmail), memory.ToArray() },
                { nameof(RetryCount), retryCount },
                { nameof(RetryWaitSeconds), retryWaitSeconds },
            },
            cancellationToken);
    }

    public static JobKey Key { get; } = new("RetryEmailJob", "RetryingJobs");
    public byte[]? SerializedEmail { get; set; }
    public int RetryCount { get; set; }
    public int RetryWaitSeconds { get; set; }

    protected override async Task ExecuteJob(IJobExecutionContext context)
    {
        if (SerializedEmail is null) throw new ArgumentNullException("email");
        var memory = new MemoryStream(SerializedEmail, writable: false);
        var email = await MimeMessage.LoadAsync(memory);
        while (RetryCount > 0)
        {
            try
            {
                await emailService.SendEmailAsync(email);
            }
            catch
            {
                RetryCount -= 1;
                await Task.Delay(TimeSpan.FromSeconds(RetryWaitSeconds));
            }
        }
    }
}
