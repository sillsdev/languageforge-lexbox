using FluentAssertions;
using LexBoxApi.Jobs;
using LexBoxApi.Services.Email;
using MimeKit;
using Moq;
using Quartz;

namespace Testing.LexBoxApi.Jobs;

public class RetryEmailJobTests
{
    private readonly Mock<IEmailService> _emailService = new();

    [Fact]
    public async Task StopsAfterFirstSuccessfulSend()
    {
        await Execute(retryCount: 3);

        _emailService.Verify(s => s.SendEmailAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetriesUntilSendSucceeds()
    {
        _emailService.SetupSequence(s => s.SendEmailAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("smtp down"))
            .Returns(Task.CompletedTask);

        await Execute(retryCount: 3);

        _emailService.Verify(s => s.SendEmailAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ThrowsAfterRetryCountFailures()
    {
        _emailService.Setup(s => s.SendEmailAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("smtp down"));

        var act = () => Execute(retryCount: 3);

        await act.Should().ThrowAsync<JobExecutionException>();
        _emailService.Verify(s => s.SendEmailAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task StopsRetryingWhenJobIsCancelled()
    {
        using var cts = new CancellationTokenSource();
        _emailService.Setup(s => s.SendEmailAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .Callback(cts.Cancel)
            .ThrowsAsync(new Exception("smtp down"));

        var act = () => Execute(retryCount: 3, cts.Token);

        (await act.Should().ThrowAsync<JobExecutionException>())
            .WithInnerException<OperationCanceledException>();
        _emailService.Verify(s => s.SendEmailAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task Execute(int retryCount, CancellationToken cancellationToken = default)
    {
        var email = new MimeMessage { Subject = "test" };
        using var memory = new MemoryStream();
        await email.WriteToAsync(memory);
        IJob job = new RetryEmailJob(_emailService.Object)
        {
            SerializedEmail = Convert.ToBase64String(memory.ToArray()),
            RetryCount = retryCount.ToString(),
            RetryWaitSeconds = "0",
        };
        await job.Execute(Mock.Of<IJobExecutionContext>(c => c.CancellationToken == cancellationToken));
    }
}
