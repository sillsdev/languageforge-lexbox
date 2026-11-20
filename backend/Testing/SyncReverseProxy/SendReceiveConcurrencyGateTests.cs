using FluentAssertions;
using LexBoxApi.Services;
using LexCore.Config;
using LexCore.ServiceInterfaces;
using LexSyncReverseProxy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Testing.SyncReverseProxy;

// Pure unit tests for the per-(user, project) concurrency gate and the version-gated rejection
// status added for the hg resumable-transport DDoS remediation. No infrastructure required.
public class SendReceiveConcurrencyGateTests
{
    private const string ProjectCode = "sena-3";
    private const string OtherProjectCode = "sena-3-sandbox";
    private const string UserA = "11111111-1111-1111-1111-111111111111";
    private const string UserB = "22222222-2222-2222-2222-222222222222";
    private const int RetryAfterSeconds = 5;

    private readonly SendReceiveService _service = new(Options.Create(
        new SendReceiveConfig { MaxConcurrentPerUserProject = 2, ConcurrencyRetryAfterSeconds = RetryAfterSeconds }));

    [Fact]
    public async Task AllowsUpToCapThenShedsTheOverflow()
    {
        var first = await _service.BeginSendReceive(ProjectCode, UserA);
        var second = await _service.BeginSendReceive(ProjectCode, UserA);

        first.Result.Should().Be(BeginSendReceiveResult.Ok);
        first.Ticket.Should().NotBeNull();
        second.Result.Should().Be(BeginSendReceiveResult.Ok);
        second.Ticket.Should().NotBeNull();

        // Cap is 2; the third concurrent request for the same (user, project) is shed.
        var third = await _service.BeginSendReceive(ProjectCode, UserA);
        third.Result.Should().Be(BeginSendReceiveResult.ConcurrencyLimitReached);
        third.Ticket.Should().BeNull();

        // Keep the first two alive until here so the gate actually sees two in-flight slots.
        GC.KeepAlive(first.Ticket);
        GC.KeepAlive(second.Ticket);
    }

    [Fact]
    public async Task ReleasingASlotAllowsTheNextRequest()
    {
        var first = await _service.BeginSendReceive(ProjectCode, UserA);
        var second = await _service.BeginSendReceive(ProjectCode, UserA);
        (await _service.BeginSendReceive(ProjectCode, UserA)).Result
            .Should().Be(BeginSendReceiveResult.ConcurrencyLimitReached);

        // Complete one in-flight request, freeing a slot.
        second.Ticket!.Dispose();

        var next = await _service.BeginSendReceive(ProjectCode, UserA);
        next.Result.Should().Be(BeginSendReceiveResult.Ok);
        next.Ticket.Should().NotBeNull();

        GC.KeepAlive(first.Ticket);
        GC.KeepAlive(next.Ticket);
    }

    [Fact]
    public async Task ADifferentUserOnTheSameProjectIsUnaffected()
    {
        // User A fills their cap for the project.
        var a1 = await _service.BeginSendReceive(ProjectCode, UserA);
        var a2 = await _service.BeginSendReceive(ProjectCode, UserA);
        (await _service.BeginSendReceive(ProjectCode, UserA)).Result
            .Should().Be(BeginSendReceiveResult.ConcurrencyLimitReached);

        // User B shares the repo but has an independent per-user budget.
        var b1 = await _service.BeginSendReceive(ProjectCode, UserB);
        b1.Result.Should().Be(BeginSendReceiveResult.Ok);
        b1.Ticket.Should().NotBeNull();

        GC.KeepAlive(a1.Ticket);
        GC.KeepAlive(a2.Ticket);
        GC.KeepAlive(b1.Ticket);
    }

    [Fact]
    public async Task TheSameUserOnADifferentProjectIsUnaffected()
    {
        // User A fills their cap for one project.
        var a1 = await _service.BeginSendReceive(ProjectCode, UserA);
        var a2 = await _service.BeginSendReceive(ProjectCode, UserA);
        (await _service.BeginSendReceive(ProjectCode, UserA)).Result
            .Should().Be(BeginSendReceiveResult.ConcurrencyLimitReached);

        // The same user syncing a different project has an independent per-project budget.
        var other = await _service.BeginSendReceive(OtherProjectCode, UserA);
        other.Result.Should().Be(BeginSendReceiveResult.Ok);
        other.Ticket.Should().NotBeNull();

        GC.KeepAlive(a1.Ticket);
        GC.KeepAlive(a2.Ticket);
        GC.KeepAlive(other.Ticket);
    }

    [Fact]
    public async Task MigrationBlocksSendReceiveAndReleasingItAllowsAgain()
    {
        // A migration holds the per-project writer lock.
        using (var migrationBlock = await _service.BlockSendReceive(ProjectCode))
        {
            migrationBlock.Should().NotBeNull();

            var gate = await _service.BeginSendReceive(ProjectCode, UserA);
            gate.Result.Should().Be(BeginSendReceiveResult.MigrationInProgress);
            gate.Ticket.Should().BeNull();
        }

        // Once the migration releases, S/R proceeds again.
        var afterMigration = await _service.BeginSendReceive(ProjectCode, UserA);
        afterMigration.Result.Should().Be(BeginSendReceiveResult.Ok);
        afterMigration.Ticket.Should().NotBeNull();
        GC.KeepAlive(afterMigration.Ticket);
    }

    [Theory]
    // Old resumable client: no capability token -> 503 (it infinite-retries on 429 but stops on 503).
    [InlineData("HgResume v03", StatusCodes.Status503ServiceUnavailable, null)]
    // Plain hgweb/mercurial client -> 503.
    [InlineData("mercurial/proto-1.0 (Mercurial 6.7)", StatusCodes.Status503ServiceUnavailable, null)]
    // Missing User-Agent -> 503.
    [InlineData(null, StatusCodes.Status503ServiceUnavailable, null)]
    [InlineData("", StatusCodes.Status503ServiceUnavailable, null)]
    // New client advertising the Chorus capability token -> 429 + Retry-After from config.
    [InlineData("HgResume v03 Chorus/5.2.0", StatusCodes.Status429TooManyRequests, RetryAfterSeconds)]
    public void ResolveConcurrencyLimitResponse_IsVersionGated(string? userAgent, int expectedStatus, int? expectedRetryAfter)
    {
        var (statusCode, retryAfterSeconds) = ProxyKernel.ResolveConcurrencyLimitResponse(userAgent, RetryAfterSeconds);
        statusCode.Should().Be(expectedStatus);
        retryAfterSeconds.Should().Be(expectedRetryAfter);
    }

    [Fact]
    public void ResolveConcurrencyLimitResponse_NeverSends429ToAnOldClient()
    {
        // The exact old-client UA string from HgResumeRestApiServer must never get a 429.
        var (statusCode, _) = ProxyKernel.ResolveConcurrencyLimitResponse("HgResume v03", RetryAfterSeconds);
        statusCode.Should().NotBe(StatusCodes.Status429TooManyRequests);
    }
}
