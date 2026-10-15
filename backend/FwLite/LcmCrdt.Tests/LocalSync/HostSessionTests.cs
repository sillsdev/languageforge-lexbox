using FwLiteShared.LocalSync.Hosting;

namespace LcmCrdt.Tests.LocalSync;

public class HostSessionTests
{
    private static readonly Guid ProjectId = Guid.Parse("3e5a7c9d-1b2f-4d6e-8a0c-4b6d8f0a2c57");
    private static readonly Guid RootCommitId = Guid.Parse("5c7e9a1b-3d4f-4b8a-9c2e-6d8f0b2d4a69");
    private const string AppVersion = "1.4.0";

    private static readonly HostedProject Hosted = new(ProjectId, "sena-3", "Sena 3", "https://lexbox.org", RootCommitId, ["seh", "en"]);

    private static HostSession NewSession() => new(Hosted, AppVersion);

    private static JoinRequest Request(string name = "Amina's phone", JoinerProject? project = null, string appVersion = AppVersion,
        int protocolVersion = LocalSyncProtocol.Version) =>
        new(name, appVersion, protocolVersion, project ?? new JoinerProject(ProjectId, RootCommitId, ["en", "seh"]));

    [Fact]
    public void AJoinRequest_WaitsForTheHostToApprove()
    {
        var session = NewSession();

        var requestId = session.RequestJoin(Request());

        session.GetJoinStatus(requestId)!.State.Should().Be(JoinState.Pending);
        session.Peers.Should().ContainSingle().Which.DisplayName.Should().Be("Amina's phone");
    }

    [Fact]
    public void AnApprovedJoiner_GetsATokenAndTheHostedProject()
    {
        var session = NewSession();
        var requestId = session.RequestJoin(Request());

        session.Approve(requestId);

        var status = session.GetJoinStatus(requestId)!;
        status.State.Should().Be(JoinState.Approved);
        status.Project.Should().Be(Hosted);
        session.IsAuthorized(status.Token!).Should().BeTrue();
    }

    [Fact]
    public void ADeclinedJoiner_GetsNoToken()
    {
        var session = NewSession();
        var requestId = session.RequestJoin(Request());

        session.Decline(requestId);

        var status = session.GetJoinStatus(requestId)!;
        status.State.Should().Be(JoinState.Rejected);
        status.Rejection.Should().Be(JoinRejection.Declined);
        status.Token.Should().BeNull();
    }

    [Fact]
    public void WithAutoApproveOn_JoinersAreApprovedStraightAway()
    {
        var session = NewSession();
        session.AutoApprove = true;

        var requestId = session.RequestJoin(Request());

        session.GetJoinStatus(requestId)!.State.Should().Be(JoinState.Approved);
    }

    [Fact]
    public void TurningAutoApproveOn_ApprovesJoinersAlreadyWaiting()
    {
        var session = NewSession();
        var requestId = session.RequestJoin(Request());

        session.AutoApprove = true;

        session.GetJoinStatus(requestId)!.State.Should().Be(JoinState.Approved);
    }

    [Fact]
    public void AJoinerWhoseCopyHasADifferentHistory_IsRefusedWithoutAsking()
    {
        var session = NewSession();
        session.AutoApprove = true;
        var impostorRoot = Guid.Parse("7a9c1e3f-5b6d-4f8a-a2c4-8e0b2d4f6a7b");

        var requestId = session.RequestJoin(Request(project: new JoinerProject(ProjectId, impostorRoot, ["seh", "en"])));

        var status = session.GetJoinStatus(requestId)!;
        status.State.Should().Be(JoinState.Rejected);
        status.Rejection.Should().Be(JoinRejection.DifferentHistory);
    }

    [Fact]
    public void AJoinerWithoutTheProject_CanBeApprovedToDownloadIt()
    {
        var session = NewSession();

        var requestId = session.RequestJoin(Request(project: null) with { Project = null });
        session.Approve(requestId);

        session.GetJoinStatus(requestId)!.State.Should().Be(JoinState.Approved);
    }

    [Fact]
    public void AJoinerOnADifferentAppVersion_IsFlaggedButNotRefused()
    {
        var session = NewSession();

        var requestId = session.RequestJoin(Request(appVersion: "1.3.0"));
        session.Approve(requestId);

        session.GetJoinStatus(requestId)!.State.Should().Be(JoinState.Approved);
        session.Peers.Single().VersionMismatch.Should().BeTrue();
    }

    [Fact]
    public void AnUnknownToken_IsNotAuthorized()
    {
        NewSession().IsAuthorized("made-up").Should().BeFalse();
    }

    [Fact]
    public void EndingTheSession_RevokesTokensAndRejectsWaitingJoiners()
    {
        var session = NewSession();
        var approved = session.RequestJoin(Request("first"));
        session.Approve(approved);
        var token = session.GetJoinStatus(approved)!.Token!;
        var waiting = session.RequestJoin(Request("second"));

        session.End();

        session.IsAuthorized(token).Should().BeFalse();
        session.GetJoinStatus(waiting)!.Rejection.Should().Be(JoinRejection.SessionEnded);
    }

    [Fact]
    public void LeavingRevokesTheJoinersToken()
    {
        var session = NewSession();
        var requestId = session.RequestJoin(Request());
        session.Approve(requestId);
        var token = session.GetJoinStatus(requestId)!.Token!;

        session.Leave(token);

        session.IsAuthorized(token).Should().BeFalse();
        session.Peers.Should().BeEmpty();
    }

    [Fact]
    public void JoinRequestsBeyondTheLimit_AreTurnedAway()
    {
        var session = NewSession();
        for (var i = 0; i < HostSession.MaxJoinRequests; i++) session.RequestJoin(Request($"device {i}"));

        var act = () => session.RequestJoin(Request("one too many"));

        act.Should().Throw<HostSessionFullException>();
        session.Peers.Should().HaveCount(HostSession.MaxJoinRequests);
    }
}
