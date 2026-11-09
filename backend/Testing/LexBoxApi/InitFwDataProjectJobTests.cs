using System.Net;
using System.Net.Http.Json;
using LexBoxApi.Jobs;
using LexBoxApi.Services;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexCore.Sync;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Testing.LexBoxApi;

/// <summary>
/// Unit tests for how one run of InitFwDataProjectJob decides what happened to a creation. Every run must be
/// safe to repeat (Quartz reruns the job after an interruption), so these cover picking up after each way an
/// earlier run could have stopped. FwHeadless is a fake HTTP handler and the repo is a mocked IHgService.
/// </summary>
public class InitFwDataProjectJobTests
{
    private const string Code = "test-proj";
    private const string PushedTip = "1234567890abcdef1234567890abcdef12345678";
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly InitFwDataProjectInput Input = new(["fr"], ["en"], "en");

    private string _tip = HgService.AllZeroHash;
    private readonly List<string> _requests = [];
    private readonly Mock<IHgService> _hgService = new();

    // Responses by request, as "METHOD path". An unlisted request throws, which AttemptInit reports as
    // RetryLater like any other exception, so the tests also check exactly which requests were made.
    private readonly Dictionary<string, Func<HttpResponseMessage>> _fwHeadless = [];

    private const string CreationStatus = "GET /api/project/creation-status";
    private const string AwaitCreation = "GET /api/project/await-creation-finished";
    private static readonly string DeleteLocalProject = $"DELETE /api/manage/project/{ProjectId}";
    private const string InitProject = "POST /api/project/initFwDataProject";

    public InitFwDataProjectJobTests()
    {
        _hgService.Setup(h => h.GetTipHash(Code, It.IsAny<CancellationToken>())).ReturnsAsync(() => _tip);
    }

    private Task<InitAttemptOutcome> Attempt()
    {
        var client = new FwHeadlessClient(new HttpClient(new FakeHandler(this)) { BaseAddress = new Uri("http://fwheadless/") },
            NullLogger<FwHeadlessClient>.Instance);
        return InitFwDataProjectJob.AttemptInit(client, _hgService.Object, NullLogger.Instance, ProjectId, Code, Input);
    }

    private static HttpResponseMessage Status(ProjectCreationStatus status) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(status) };

    private static HttpResponseMessage Response(HttpStatusCode code, string body = "") =>
        new(code) { Content = new StringContent(body) };

    [Fact]
    public async Task A_project_whose_push_already_landed_is_created_without_asking_FwHeadless()
    {
        _tip = PushedTip;

        var outcome = await Attempt();

        outcome.Result.Should().Be(InitAttemptResult.Created);
        _requests.Should().BeEmpty("the repo already shows that an earlier run's creation succeeded");
    }

    [Fact]
    public async Task Waits_for_a_creation_still_running_in_FwHeadless_instead_of_starting_another()
    {
        _fwHeadless[CreationStatus] = () => Status(ProjectCreationStatus.Creating);
        _fwHeadless[AwaitCreation] = () =>
        {
            _tip = PushedTip;
            return Status(ProjectCreationStatus.Created);
        };

        var outcome = await Attempt();

        outcome.Result.Should().Be(InitAttemptResult.Created);
        _requests.Should().Equal(CreationStatus, AwaitCreation);
    }

    [Fact]
    public async Task A_creation_FwHeadless_reports_as_failed_while_waiting_is_a_failure()
    {
        _fwHeadless[CreationStatus] = () => Status(ProjectCreationStatus.Creating);
        _fwHeadless[AwaitCreation] = () => Status(ProjectCreationStatus.Failed("the push failed"));

        var outcome = await Attempt();

        outcome.Should().Be(InitAttemptOutcome.Failed("the push failed"));
    }

    [Fact]
    public async Task A_creation_still_running_after_the_wait_is_retried_later()
    {
        _fwHeadless[CreationStatus] = () => Status(ProjectCreationStatus.Creating);
        _fwHeadless[AwaitCreation] = () => Status(ProjectCreationStatus.TimedOutAwaitingCreation);

        var outcome = await Attempt();

        outcome.Result.Should().Be(InitAttemptResult.RetryLater);
        _requests.Should().Equal(CreationStatus, AwaitCreation);
    }

    [Fact]
    public async Task Clears_FwHeadless_local_state_before_starting_a_creation()
    {
        _fwHeadless[CreationStatus] = () => Response(HttpStatusCode.NotFound);
        _fwHeadless[DeleteLocalProject] = () => Response(HttpStatusCode.OK);
        _fwHeadless[InitProject] = () => Response(HttpStatusCode.OK);

        var outcome = await Attempt();

        outcome.Result.Should().Be(InitAttemptResult.Created);
        _requests.Should().Equal(CreationStatus, DeleteLocalProject, InitProject);
    }

    [Fact]
    public async Task FwHeadless_being_busy_with_the_project_is_retried_later_without_starting_a_creation()
    {
        _fwHeadless[CreationStatus] = () => Status(ProjectCreationStatus.NotCreated);
        _fwHeadless[DeleteLocalProject] = () => Response(HttpStatusCode.Conflict);

        var outcome = await Attempt();

        outcome.Result.Should().Be(InitAttemptResult.RetryLater);
        _requests.Should().Equal(CreationStatus, DeleteLocalProject);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, InitAttemptResult.RetryLater)] // something else holds the project, so nothing was built
    [InlineData(HttpStatusCode.BadRequest, InitAttemptResult.Failed)]
    [InlineData(HttpStatusCode.InternalServerError, InitAttemptResult.Failed)]
    public async Task An_answer_from_FwHeadless_decides_the_outcome(HttpStatusCode statusCode, InitAttemptResult expected)
    {
        _fwHeadless[CreationStatus] = () => Status(ProjectCreationStatus.NotCreated);
        _fwHeadless[DeleteLocalProject] = () => Response(HttpStatusCode.OK);
        _fwHeadless[InitProject] = () => Response(statusCode, "what went wrong");

        var outcome = await Attempt();

        outcome.Should().Be(new InitAttemptOutcome(expected, "what went wrong"));
        _requests.Should().Equal(CreationStatus, DeleteLocalProject, InitProject);
    }

    [Fact]
    public async Task No_answer_from_FwHeadless_is_retried_later_because_it_may_still_be_creating()
    {
        _fwHeadless[CreationStatus] = () => Status(ProjectCreationStatus.NotCreated);
        _fwHeadless[DeleteLocalProject] = () => Response(HttpStatusCode.OK);
        _fwHeadless[InitProject] = () => throw new HttpRequestException("Connection refused");

        var outcome = await Attempt();

        outcome.Result.Should().Be(InitAttemptResult.RetryLater);
        _requests.Should().Equal(CreationStatus, DeleteLocalProject, InitProject);
    }

    [Fact]
    public async Task Not_being_able_to_read_the_repo_is_retried_later()
    {
        _hgService.Setup(h => h.GetTipHash(Code, It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("hg is down"));

        var outcome = await Attempt();

        outcome.Result.Should().Be(InitAttemptResult.RetryLater);
        _requests.Should().BeEmpty();
    }

    [Fact]
    public void The_creation_input_survives_the_job_store()
    {
        // The job store only takes strings, so the input travels as JSON; it's a positional record with
        // IReadOnlyList properties, which System.Text.Json has to bind through the constructor
        var roundTripped = System.Text.Json.JsonSerializer.Deserialize<InitFwDataProjectInput>(
            System.Text.Json.JsonSerializer.Serialize(new InitFwDataProjectInput(["fr", "es"], ["en", "de"], "pt")));

        roundTripped.Should().NotBeNull();
        roundTripped!.WsVernacular.Should().Equal("fr", "es");
        roundTripped.WsAnalysis.Should().Equal("en", "de");
        roundTripped.WsUi.Should().Be("pt");
    }

    private class FakeHandler(InitFwDataProjectJobTests test) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = $"{request.Method} {request.RequestUri!.AbsolutePath}";
            test._requests.Add(key);
            if (!test._fwHeadless.TryGetValue(key, out var respond))
                throw new InvalidOperationException($"Unexpected request to FwHeadless: {key}");
            return Task.FromResult(respond());
        }
    }
}
