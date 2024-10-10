using System.Text.Json;
using LcmCrdt.Changes;
using LcmCrdt.Objects;
using MiniLcm.Models;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace LcmCrdt.Tests;

public class JsonPatchRewriteTests
{
    private JsonPatchDocument<MiniLcm.Models.Sense> _patchDocument = new() { Options = new JsonSerializerOptions(JsonSerializerDefaults.Web) };

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
        _patchDocument.Replace(s => s.PartOfSpeechId, newPartOfSpeechId);
        _patchDocument.Replace(s => s.Gloss["en"], "new gloss");

        var changes = _sense.ToChanges(_patchDocument).ToArray();

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
        _patchDocument.Replace(s => s.PartOfSpeechId, newPartOfSpeechId);

        var changes = _sense.ToChanges(_patchDocument).ToArray();

        var setPartOfSpeechChange = changes.Should().ContainSingle()
            .Subject.Should().BeOfType<SetPartOfSpeechChange>().Subject;
        setPartOfSpeechChange.EntityId.Should().Be(_sense.Id);
        setPartOfSpeechChange.PartOfSpeechId.Should().Be(newPartOfSpeechId);
    }

    [Fact]
    public void RewritesAddSemanticDomainChangesIntoAddSemanticDomainChange()
    {
        var newSemanticDomainId = Guid.NewGuid();
        _patchDocument.Add(s => s.SemanticDomains,
            new SemanticDomain() { Id = newSemanticDomainId, Code = "new code", Name = new MultiString() });

        var changes = _sense.ToChanges(_patchDocument).ToArray();

        var addSemanticDomainChange = (AddSemanticDomainChange)changes.Should().AllBeOfType<AddSemanticDomainChange>().And.ContainSingle().Subject;
        addSemanticDomainChange.EntityId.Should().Be(_sense.Id);
        addSemanticDomainChange.SemanticDomain.Id.Should().Be(newSemanticDomainId);
    }

    [Fact]
    public void RewritesAddSemanticDomainChangesIntoAddSemanticDomainChange_JsonElement()
    {
        var newSemanticDomainId = Guid.Parse("46e4fe08-ffa0-4c8b-bf88-2c56138904d1");
        _patchDocument.Operations.Add(new Operation<MiniLcm.Models.Sense>("add", "/semanticDomains/-", null,
            JsonSerializer.Deserialize<JsonElement>("""{"deletedAt":null,"predefined":true,"id":"46e4fe08-ffa0-4c8b-bf88-2c56138904d1","name":{"en":"Sky"},"code":"1.1"}""")));

        var changes = _sense.ToChanges(_patchDocument).ToArray();

        var addSemanticDomainChange = (AddSemanticDomainChange)changes.Should().AllBeOfType<AddSemanticDomainChange>().And.ContainSingle().Subject;
        addSemanticDomainChange.EntityId.Should().Be(_sense.Id);
        addSemanticDomainChange.SemanticDomain.Id.Should().Be(newSemanticDomainId);
    }

    [Fact]
    public void RewritesReplaceSemanticDomainPatchChangesIntoReplaceSemanticDomainChange()
    {
        var oldSemanticDomainId = _sense.SemanticDomains[0].Id;
        var newSemanticDomainId = Guid.NewGuid();
        _patchDocument.Replace(s => s.SemanticDomains, new SemanticDomain() { Id = newSemanticDomainId, Code = "new code", Name = new MultiString() }, 0);

        var changes = _sense.ToChanges(_patchDocument).ToArray();

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
        _patchDocument.Replace(s => s.SemanticDomains, new SemanticDomain() { Id = newSemanticDomainId, Code = "new code", Name = new MultiString() });

        var changes = _sense.ToChanges(_patchDocument).ToArray();

        var replaceSemanticDomainChange = (ReplaceSemanticDomainChange)changes.Should().AllBeOfType<ReplaceSemanticDomainChange>().And.ContainSingle().Subject;
        replaceSemanticDomainChange.EntityId.Should().Be(_sense.Id);
        replaceSemanticDomainChange.SemanticDomain.Id.Should().Be(newSemanticDomainId);
        replaceSemanticDomainChange.OldSemanticDomainId.Should().Be(oldSemanticDomainId);
    }

    [Fact]
    public void RewritesRemoveSemanticDomainPatchChangesIntoReplaceSemanticDomainChange()
    {
        var semanticDomainIdToRemove = _sense.SemanticDomains[0].Id;
        _patchDocument.Remove(s => s.SemanticDomains, 0);

        var changes = _sense.ToChanges(_patchDocument).ToArray();

        var removeSemanticDomainChange = (RemoveSemanticDomainChange)changes.Should().AllBeOfType<
            RemoveSemanticDomainChange>().And.ContainSingle().Subject;
        removeSemanticDomainChange.EntityId.Should().Be(_sense.Id);
        removeSemanticDomainChange.SemanticDomainId.Should().Be(semanticDomainIdToRemove);
    }

    [Fact]
    public void RewritesRemoveNoIndexSemanticDomainPatchChangesIntoReplaceSemanticDomainChange()
    {
        var semanticDomainIdToRemove = _sense.SemanticDomains[0].Id;
        _patchDocument.Remove(s => s.SemanticDomains);

        var changes = _sense.ToChanges(_patchDocument).ToArray();

        var removeSemanticDomainChange = (RemoveSemanticDomainChange)changes.Should().AllBeOfType<
            RemoveSemanticDomainChange>().And.ContainSingle().Subject;
        removeSemanticDomainChange.EntityId.Should().Be(_sense.Id);
        removeSemanticDomainChange.SemanticDomainId.Should().Be(semanticDomainIdToRemove);
    }
}
