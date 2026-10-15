using LcmCrdt.LocalSync;

namespace LcmCrdt.Tests.LocalSync;

public class ProjectIdentityComparisonTests
{
    private static readonly Guid ProjectId = Guid.Parse("8f2c1a52-5d0e-4c43-9e1b-2a7d3f6b9c10");
    private static readonly Guid RootCommitId = Guid.Parse("1b6e9d40-73a2-4f8e-b5c1-0d9a8e7f6c21");

    private static ProjectIdentity Identity(Guid? projectId = null, Guid? rootCommitId = null, params string[] wsIds)
    {
        return new ProjectIdentity(projectId ?? ProjectId,
            rootCommitId ?? RootCommitId,
            new HashSet<string>(wsIds.Length == 0 ? ["seh", "en"] : wsIds));
    }

    [Fact]
    public void NoLocalProject_WhenThereIsNoLocalIdentity()
    {
        ProjectIdentity.Compare(null, Identity()).Should().Be(IdentityComparison.NoLocalProject);
    }

    [Fact]
    public void DifferentProject_WhenProjectIdsDiffer()
    {
        var local = Identity(projectId: Guid.Parse("c3d8e2f1-0a4b-4c6d-8e9f-1a2b3c4d5e6f"));
        ProjectIdentity.Compare(local, Identity()).Should().Be(IdentityComparison.DifferentProject);
    }

    [Fact]
    public void Match_WhenIdRootAndWritingSystemsAgree()
    {
        // writing system order must not matter
        var local = Identity(wsIds: ["en", "seh"]);
        var remote = Identity(wsIds: ["seh", "en"]);
        ProjectIdentity.Compare(local, remote).Should().Be(IdentityComparison.Match);
    }

    [Fact]
    public void DifferentRootCommit_WhenSameIdButDifferentHistory()
    {
        var local = Identity(rootCommitId: Guid.Parse("e5f60718-293a-4b5c-8d7e-9f0a1b2c3d4e"));
        ProjectIdentity.Compare(local, Identity()).Should().Be(IdentityComparison.DifferentRootCommit);
    }

    [Fact]
    public void Match_WhenOneSideHasAddedAWritingSystem()
    {
        // a writing system added on one device must be able to reach the others through local sync
        var local = Identity(wsIds: ["seh", "en", "pt"]);
        ProjectIdentity.Compare(local, Identity()).Should().Be(IdentityComparison.Match);
    }

    [Fact]
    public void DifferentWritingSystems_WhenTheyShareNone()
    {
        var local = Identity(wsIds: ["mal", "fr"]);
        ProjectIdentity.Compare(local, Identity()).Should().Be(IdentityComparison.DifferentWritingSystems);
    }

    [Fact]
    public void Match_WhenOneSideHasNoWritingSystemsYet()
    {
        var local = new ProjectIdentity(ProjectId, RootCommitId, new HashSet<string>());
        ProjectIdentity.Compare(local, Identity()).Should().Be(IdentityComparison.Match);
    }
}
