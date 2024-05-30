namespace LcmCrdt.Tests.Mocks;

public class MockProjectContext(CrdtProject project) : ProjectContext
{
    public override CrdtProject? Project { get; set; } = project;
}
