using LcmCrdt.Changes;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace LcmCrdt.Tests.Changes;

public class JsonPatchChangeTests
{
    [Fact]
    public void NewChangeDirect_ThrowsForRemoveAtIndex()
    {
        var patch = new JsonPatchDocument<Entry>();
        patch.Operations.Add(new Operation<Entry>("remove", "/senses/1", null, null));
        var act = () => new JsonPatchChange<Entry>(Guid.NewGuid(), patch);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void NewChangeIPatchDoc_ThrowsForRemoveAtIndex()
    {
        var patch = new JsonPatchDocument<Entry>();
        patch.Operations.Add(new Operation<Entry>("remove", "/senses/1", null, null));
        var act = () => new JsonPatchChange<Entry>(Guid.NewGuid(), patch);
        act.Should().Throw<NotSupportedException>();
    }
}
