using LexBoxApi.Jobs;
using LexBoxApi.Services;
using LexBoxApi.Services.Email;
using LexCore.Config;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;
using Testing.Fixtures;

namespace Testing.LexBoxApi.Jobs;

[Collection(nameof(TestingServicesFixture))]
[Trait("Category", "RequiresDb")]
public class InitFwDataProjectJobDbTests(TestingServicesFixture testing)
{
    [Fact]
    public async Task A_failure_marking_the_project_ready_is_retried()
    {
        var serviceProvider = testing.ConfigureServices();
        var dbContext = serviceProvider.GetRequiredService<LexBoxDbContext>();
        var hgService = new Mock<IHgService>();
        // The push landed, so the attempt counts as created without asking FwHeadless
        hgService.Setup(h => h.GetTipHash(It.IsAny<ProjectCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1234567890abcdef1234567890abcdef12345678");
        hgService.Setup(h => h.GetLastCommitTimeFromHg(It.IsAny<ProjectCode>()))
            .ThrowsAsync(new HttpRequestException("hgweb blip"));
        var projectService = new ProjectService(dbContext, hgService.Object,
            serviceProvider.GetRequiredService<IOptions<HgConfig>>(),
            Mock.Of<IMemoryCache>(), Mock.Of<IEmailService>(), null!);
        var projectId = await projectService.CreateProject(
            new(null, "Init finalize retry", "", "init-finalize-retry", ProjectType.FLEx, RetentionPolicy.Test, false, null, null),
            fwDataInitStatus: FwDataInitStatus.InProgress);
        var scheduler = new Mock<IScheduler>();
        var schedulerFactory = Mock.Of<ISchedulerFactory>(f => f.GetScheduler(It.IsAny<CancellationToken>()) == Task.FromResult(scheduler.Object));
        IJob job = new InitFwDataProjectJob(dbContext, projectService,
            new FwHeadlessClient(new HttpClient(), NullLogger<FwHeadlessClient>.Instance),
            hgService.Object, schedulerFactory, Mock.Of<IMemoryCache>(), NullLogger<InitFwDataProjectJob>.Instance)
        {
            ProjectId = projectId.ToString(),
            Input = System.Text.Json.JsonSerializer.Serialize(new InitFwDataProjectInput(["fr"], ["en"], "en")),
            Attempt = "1",
        };

        await job.Execute(Mock.Of<IJobExecutionContext>());

        hgService.Verify(h => h.GetLastCommitTimeFromHg(It.IsAny<ProjectCode>()), Times.Once);
        scheduler.Verify(s => s.ScheduleJob(
            It.Is<ITrigger>(t => t.JobDataMap.GetString(nameof(InitFwDataProjectJob.Attempt)) == "2"),
            It.IsAny<CancellationToken>()), Times.Once);
        var project = await dbContext.Projects.AsNoTracking().SingleAsync(p => p.Id == projectId);
        project.FwDataInitStatus.Should().Be(FwDataInitStatus.InProgress);
    }
}
