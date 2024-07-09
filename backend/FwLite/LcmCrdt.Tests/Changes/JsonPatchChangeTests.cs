using System.Text.Json;
using Crdt.Entities;
using LcmCrdt.Changes;
using LcmCrdt.Objects;
using SystemTextJsonPatch;

namespace LcmCrdt.Tests.Changes;

public class JsonPatchChangeTests
{
    [Fact]
    public void NewChangeAction_ThrowsForRemoveAtIndex()
    {
        var act = () => new JsonPatchChange<Entry>(Guid.NewGuid(),
            patch =>
            {
                patch.Remove(entry => entry.Senses, 1);
            });
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void NewChangeDirect_ThrowsForRemoveAtIndex()
    {
        var patch = new JsonPatchDocument<Entry>();
        patch.Remove(entry => entry.Senses, 1);
        var act = () => new JsonPatchChange<Entry>(Guid.NewGuid(), patch);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void NewChangeIPatchDoc_ThrowsForRemoveAtIndex()
    {
        var patch = new JsonPatchDocument<Entry>();
        patch.Remove(entry => entry.Senses, 1);
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
