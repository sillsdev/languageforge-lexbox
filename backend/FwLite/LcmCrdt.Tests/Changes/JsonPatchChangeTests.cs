using System.Text.Json;
using Crdt.Entities;
using LcmCrdt.Changes;
using LcmCrdt.Objects;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace LcmCrdt.Tests.Changes;

public class JsonPatchChangeTests
{
    [Fact]
    public void NewChangeAction_ThrowsForRemoveAtIndex()
    {
        var act = () => new JsonPatchChange<Entry>(Guid.NewGuid(),
            patch =>
            {
                patch.Operations.Add(new Operation<Entry>("remove", "/senses/1", null, null));
            });
        act.Should().Throw<NotSupportedException>();
    }

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
        var act = () => new JsonPatchChange<Entry>(Guid.NewGuid(), patch, JsonSerializerOptions.Default);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void NewPatchDoc_ThrowsForIndexBasedPath()
    {
        var patch = new JsonPatchDocument<Entry>();
        patch.Replace(entry => entry.Senses[0].PartOfSpeech, "noun");
        var act = () => new JsonPatchChange<Entry>(Guid.NewGuid(), patch);
        act.Should().Throw<NotSupportedException>();
    }
}
