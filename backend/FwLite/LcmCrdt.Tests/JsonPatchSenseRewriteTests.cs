using LcmCrdt.Changes;
using MiniLcm.Models;
using SystemTextJsonPatch;
using SemanticDomain = LcmCrdt.Objects.SemanticDomain;
using Sense = LcmCrdt.Objects.Sense;

namespace LcmCrdt.Tests;

public class JsonPatchSenseRewriteTests
{
    private Sense _sense = new Sense()
    {
        Id = Guid.NewGuid(),
        EntryId = Guid.NewGuid(),
        PartOfSpeechId = Guid.NewGuid(),
        PartOfSpeech = "test",
        SemanticDomains = [new SemanticDomain() { Id = Guid.NewGuid(), Code = "test", Name = new MultiString() }],
    };

    [Fact]
    public void RewritePartOfSpeechChangesIntoSetPartOfSpeechChange()
    {
        var newPartOfSpeechId = Guid.NewGuid();
        var patchDocument = new JsonPatchDocument<MiniLcm.Models.Sense>();
        patchDocument.Replace(s => s.PartOfSpeechId, newPartOfSpeechId);
        patchDocument.Replace(s => s.Gloss["en"], "new gloss");

        var changes = Sense.ChangesFromJsonPatch(_sense, patchDocument).ToArray();

        var setPartOfSpeechChange = changes.OfType<SetPartOfSpeechChange>().Should().ContainSingle().Subject;
        setPartOfSpeechChange.EntityId.Should().Be(_sense.Id);
        setPartOfSpeechChange.PartOfSpeechId.Should().Be(newPartOfSpeechId);

        var patchChange = changes.OfType<JsonPatchChange<Sense>>().Should().ContainSingle().Subject;
        patchChange.EntityId.Should().Be(_sense.Id);
        patchChange.PatchDocument.Operations.Should().ContainSingle().Subject.Value.Should().Be("new gloss");
    }

    [Fact]
    public void JsonPatchChangeRewriteDoesNotReturnEmptyPatchChanges()
    {
        var newPartOfSpeechId = Guid.NewGuid();
        var patchDocument = new JsonPatchDocument<MiniLcm.Models.Sense>();
        patchDocument.Replace(s => s.PartOfSpeechId, newPartOfSpeechId);

        var changes = Sense.ChangesFromJsonPatch(_sense, patchDocument).ToArray();

        var setPartOfSpeechChange = changes.Should().ContainSingle()
            .Subject.Should().BeOfType<SetPartOfSpeechChange>().Subject;
        setPartOfSpeechChange.EntityId.Should().Be(_sense.Id);
        setPartOfSpeechChange.PartOfSpeechId.Should().Be(newPartOfSpeechId);
    }

    [Fact]
    public void RewritesAddSemanticDomainChangesIntoAddSemanticDomainChange()
    {
        var newSemanticDomainId = Guid.NewGuid();
        var patchDocument = new JsonPatchDocument<MiniLcm.Models.Sense>();
        patchDocument.Add(s => s.SemanticDomains,
            new SemanticDomain() { Id = newSemanticDomainId, Code = "new code", Name = new MultiString() });

        var changes = Sense.ChangesFromJsonPatch(_sense, patchDocument).ToArray();

        var addSemanticDomainChange = (AddSemanticDomainChange)changes.Should().AllBeOfType<AddSemanticDomainChange>().And.ContainSingle().Subject;
        addSemanticDomainChange.EntityId.Should().Be(_sense.Id);
        addSemanticDomainChange.SemanticDomain.Id.Should().Be(newSemanticDomainId);
    }

    [Fact]
    public void RewritesReplaceSemanticDomainPatchChangesIntoReplaceSemanticDomainChange()
    {
        var oldSemanticDomainId = _sense.SemanticDomains[0].Id;
        var newSemanticDomainId = Guid.NewGuid();
        var patchDocument = new JsonPatchDocument<MiniLcm.Models.Sense>();
        patchDocument.Replace(s => s.SemanticDomains, new SemanticDomain() { Id = newSemanticDomainId, Code = "new code", Name = new MultiString() }, 0);

        var changes = Sense.ChangesFromJsonPatch(_sense, patchDocument).ToArray();

        var replaceSemanticDomainChange = (ReplaceSemanticDomainChange)changes.Should().AllBeOfType<ReplaceSemanticDomainChange>().And.ContainSingle().Subject;
        replaceSemanticDomainChange.EntityId.Should().Be(_sense.Id);
        replaceSemanticDomainChange.SemanticDomain.Id.Should().Be(newSemanticDomainId);
        replaceSemanticDomainChange.OldSemanticDomainId.Should().Be(oldSemanticDomainId);
    }
    [Fact]
    public void RewritesReplaceNoIndexSemanticDomainPatchChangesIntoReplaceSemanticDomainChange()
    {
        var oldSemanticDomainId = _sense.SemanticDomains[0].Id;
        var newSemanticDomainId = Guid.NewGuid();
        var patchDocument = new JsonPatchDocument<MiniLcm.Models.Sense>();
        patchDocument.Replace(s => s.SemanticDomains, new SemanticDomain() { Id = newSemanticDomainId, Code = "new code", Name = new MultiString() });

        var changes = Sense.ChangesFromJsonPatch(_sense, patchDocument).ToArray();

        var replaceSemanticDomainChange = (ReplaceSemanticDomainChange)changes.Should().AllBeOfType<ReplaceSemanticDomainChange>().And.ContainSingle().Subject;
        replaceSemanticDomainChange.EntityId.Should().Be(_sense.Id);
        replaceSemanticDomainChange.SemanticDomain.Id.Should().Be(newSemanticDomainId);
        replaceSemanticDomainChange.OldSemanticDomainId.Should().Be(oldSemanticDomainId);
    }

    [Fact]
    public void RewritesRemoveSemanticDomainPatchChangesIntoReplaceSemanticDomainChange()
    {
        var patchDocument = new JsonPatchDocument<MiniLcm.Models.Sense>();
        var semanticDomainIdToRemove = _sense.SemanticDomains[0].Id;
        patchDocument.Remove(s => s.SemanticDomains, 0);

        var changes = Sense.ChangesFromJsonPatch(_sense, patchDocument).ToArray();

        var removeSemanticDomainChange = (RemoveSemanticDomainChange)changes.Should().AllBeOfType<
            RemoveSemanticDomainChange>().And.ContainSingle().Subject;
        removeSemanticDomainChange.EntityId.Should().Be(_sense.Id);
        removeSemanticDomainChange.SemanticDomainId.Should().Be(semanticDomainIdToRemove);
    }

    [Fact]
    public void RewritesRemoveNoIndexSemanticDomainPatchChangesIntoReplaceSemanticDomainChange()
    {
        var patchDocument = new JsonPatchDocument<MiniLcm.Models.Sense>();
        var semanticDomainIdToRemove = _sense.SemanticDomains[0].Id;
        patchDocument.Remove(s => s.SemanticDomains);

        var changes = Sense.ChangesFromJsonPatch(_sense, patchDocument).ToArray();

        var removeSemanticDomainChange = (RemoveSemanticDomainChange)changes.Should().AllBeOfType<
            RemoveSemanticDomainChange>().And.ContainSingle().Subject;
        removeSemanticDomainChange.EntityId.Should().Be(_sense.Id);
        removeSemanticDomainChange.SemanticDomainId.Should().Be(semanticDomainIdToRemove);
    }
}
