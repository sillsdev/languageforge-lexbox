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
        var serializedEmail = Convert.ToBase64String(memory.ToArray());
        await QueueJob(schedulerFactory,
            Key,
            new JobDataMap {
                { nameof(SerializedEmail), serializedEmail },
                { nameof(RetryCount), retryCount.ToString() },
                { nameof(RetryWaitSeconds), retryWaitSeconds.ToString() },
            },
            cancellationToken);
    }

    public static JobKey Key { get; } = new("RetryEmailJob", "RetryingJobs");
    public string? SerializedEmail { get; set; }
    public string? RetryCount { get; set; }
    public string? RetryWaitSeconds { get; set; }

    protected override async Task ExecuteJob(IJobExecutionContext context)
    {
        ArgumentException.ThrowIfNullOrEmpty(SerializedEmail, "email");
        ArgumentException.ThrowIfNullOrEmpty(RetryCount, nameof(RetryCount));
        ArgumentException.ThrowIfNullOrEmpty(RetryWaitSeconds, nameof(RetryWaitSeconds));
        if (!int.TryParse(RetryCount, out int retryCount)) throw new ArgumentException($"Invalid number {RetryCount}", nameof(RetryCount));
        if (!int.TryParse(RetryWaitSeconds, out int retryWaitSeconds)) throw new ArgumentException($"Invalid number {RetryWaitSeconds}", nameof(RetryWaitSeconds));
        var memory = new MemoryStream(Convert.FromBase64String(SerializedEmail), writable: false);
        var email = await MimeMessage.LoadAsync(memory);
        while (retryCount > 0)
        {
            try
            {
                await emailService.SendEmailAsync(email);
            }
            catch
            {
                retryCount -= 1;
                await Task.Delay(TimeSpan.FromSeconds(retryWaitSeconds));
            }
        }
    }
}
