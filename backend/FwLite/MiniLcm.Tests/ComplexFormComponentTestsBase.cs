using FluentAssertions.Execution;
using MiniLcm.Models;

namespace MiniLcm.Tests;

public abstract class ComplexFormComponentTestsBase : MiniLcmTestBase
{
    protected readonly Guid _complexFormEntryId = Guid.NewGuid();
    protected readonly Guid _componentEntryId = Guid.NewGuid();
    protected readonly Guid _componentSenseId1 = Guid.NewGuid();
    protected readonly Guid _componentSenseId2 = Guid.NewGuid();
    private Entry _complexFormEntry = null!;
    private Entry _componentEntry = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _complexFormEntry = await Api.CreateEntry(new()
        {
            Id = _complexFormEntryId,
            LexemeForm = { { "en", "complex form" } }
        });
        _componentEntry = await Api.CreateEntry(new()
        {
            Id = _componentEntryId,
            LexemeForm = { { "en", "component" } },
            Senses =
            [
                new Sense
                {
                    Id = _componentSenseId1,
                    Gloss = { { "en", "component sense 1" } }
                },
                new Sense
                {
                    Id = _componentSenseId2,
                    Gloss = { { "en", "component sense 2" } }
                }
            ]
        });
    }

    [Fact]
    public async Task CreateComplexFormComponent_Works()
    {
        var component = await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, _componentEntry));
        component.ComplexFormEntryId.Should().Be(_complexFormEntryId);
        component.ComponentEntryId.Should().Be(_componentEntryId);
        component.ComponentSenseId.Should().BeNull();
        component.ComplexFormHeadword.Should().Be("complex form");
        component.ComponentHeadword.Should().Be("component");
    }

    [Fact]
    public async Task RemoveComplexFormComponent_Works()
    {
        var component = await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, _componentEntry));
        component.ComplexFormEntryId.Should().Be(_complexFormEntryId);
        component.ComponentEntryId.Should().Be(_componentEntryId);
        await Api.DeleteComplexFormComponent(component);
        var entries = await Api.GetEntries().ToArrayAsync();
        var complexFormEntry = entries.Should().ContainSingle(e => e.Id == _complexFormEntryId).Subject;
        var componentEntry = entries.Should().ContainSingle(e => e.Id == _componentEntryId).Subject;
        complexFormEntry.Components.Should().BeEmpty();
        componentEntry.ComplexForms.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEntries_Works()
    {
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, _componentEntry));
        var entries = await Api.GetEntries().ToArrayAsync();
        var complexFormEntry = entries.Should().ContainSingle(e => e.Id == _complexFormEntryId).Subject;
        var componentEntry = entries.Should().ContainSingle(e => e.Id == _componentEntryId).Subject;
        complexFormEntry.Components.Should().ContainSingle(r => r.ComponentEntryId == _componentEntryId);
        componentEntry.ComplexForms.Should().ContainSingle(r => r.ComplexFormEntryId == _complexFormEntryId);
    }

    [Fact]
    public async Task CreateComplexFormComponent_UsingTheSameComponentWithNullSenseDoesNothing()
    {
        var component1 = await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, _componentEntry));
        var component2 = await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, _componentEntry));
        component2.Should().BeEquivalentTo(component1);
    }

    [Fact]
    public async Task CreateComplexFormComponent_UsingTheSameComponentWithSenseDoesNothing()
    {
        var component1 = await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, _componentEntry, _componentSenseId1));
        var component2 = await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, _componentEntry, _componentSenseId1));
        component2.Should().BeEquivalentTo(component1);
    }

    [Fact]
    public async Task CreateComplexFormComponent_WorksWithSense()
    {
        var component = await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, _componentEntry, _componentSenseId1));
        component.ComplexFormEntryId.Should().Be(_complexFormEntryId);
        component.ComponentEntryId.Should().Be(_componentEntryId);
        component.ComponentSenseId.Should().Be(_componentSenseId1);
        component.ComplexFormHeadword.Should().Be("complex form");
        component.ComponentHeadword.Should().Be("component");
    }

    [Fact]
    public async Task CreateComplexFormComponent_CanCreateMultipleComponentSenses()
    {
        var component1 = await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, _componentEntry, _componentSenseId1));
        component1.ComplexFormEntryId.Should().Be(_complexFormEntryId);
        component1.ComponentEntryId.Should().Be(_componentEntryId);
        component1.ComponentSenseId.Should().Be(_componentSenseId1);

        var component2 = await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, _componentEntry, _componentSenseId2));
        component2.ComplexFormEntryId.Should().Be(_complexFormEntryId);
        component2.ComponentEntryId.Should().Be(_componentEntryId);
        component2.ComponentSenseId.Should().Be(_componentSenseId2);
    }

    [Fact]
    public async Task CreateComplexFormComponent_ThrowsWhenMakingASimpleReferenceCycle()
    {
        var act = async () => await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_componentEntry, _componentEntry));
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateComplexFormComponent_ThrowsWhenMakingA2LayerReferenceCycle()
    {
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, _componentEntry));
        var act = async () => await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_componentEntry, _complexFormEntry));
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateComplexFormComponent_ThrowsWhenMakingA3LayerReferenceCycle()
    {
        var entry3 = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(), LexemeForm = { { "en", "entry3" } }
        });
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, entry3));
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(entry3, _componentEntry));
        var act = async () => await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_componentEntry, _complexFormEntry));
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateComplexFormComponent_WorksWhenAComponentWasDeletedWhichWouldCauseACycle()
    {
        var entry3 = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(), LexemeForm = { { "en", "entry3" } }
        });
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, entry3));
        var complexFormComponent = await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(entry3, _componentEntry));
        await Api.DeleteComplexFormComponent(complexFormComponent);
        var act = async () => await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_componentEntry, _complexFormEntry));
        await act.Should().NotThrowAsync("a component was deleted which was part of the cycle");
    }

    [Fact]
    public async Task CreateComplexFormType_Works()
    {
        var complexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new() { { "en", "test" } } };
        await Api.CreateComplexFormType(complexFormType);
        var types = await Api.GetComplexFormTypes().ToArrayAsync();
        types.Should().ContainSingle(t => t.Id == complexFormType.Id);
    }

    [Fact]
    public async Task UpdateComplexFormType_Works()
    {
        var complexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new() { { "en", "test" } } };
        await Api.CreateComplexFormType(complexFormType);
        var updatedComplexFormType = await Api.UpdateComplexFormType(complexFormType.Id, new UpdateObjectInput<ComplexFormType>().Set(c => c.Name["en"], "updated"));
        updatedComplexFormType.Name["en"].Should().Be("updated");
    }

    [Fact]
    public async Task UpdateComplexFormTypeSync_Works()
    {
        var complexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new() { { "en", "test" } } };
        await Api.CreateComplexFormType(complexFormType);
        var afterFormType = complexFormType with { Name = new() { { "en", "updated" } } };
        var actualFormType = await Api.UpdateComplexFormType(complexFormType, afterFormType);
        actualFormType.Should().BeEquivalentTo(afterFormType, options => options.Excluding(c => c.Id));
    }

    [Fact]
    public async Task AddComplexFormType_Works()
    {
        var complexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new() { { "en", "test" } } };
        await Api.CreateComplexFormType(complexFormType);
        await Api.AddComplexFormType(_complexFormEntryId, complexFormType.Id);
        var entry = await Api.GetEntry(_complexFormEntryId);
        entry.Should().NotBeNull();
        entry!.ComplexFormTypes.Should().ContainSingle(c => c.Id == complexFormType.Id);
    }

    [Fact]
    public async Task RemoveComplexFormType_Works()
    {
        await AddComplexFormType_Works();
        var entry = await Api.GetEntry(_complexFormEntryId);
        await Api.RemoveComplexFormType(_complexFormEntryId, entry!.ComplexFormTypes[0].Id);
        entry = await Api.GetEntry(_complexFormEntryId);
        entry.Should().NotBeNull();
        entry!.ComplexFormTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveComplexFormType_WorksWhenTypeDoesNotExist()
    {
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, _componentEntry));
        await Api.RemoveComplexFormType(_complexFormEntryId, Guid.NewGuid());
    }

    [Fact]
    public async Task RemoveComplexFormType_WorksWhenTypeIsNotOnEntry()
    {
        //FW projects react differently if an entry has complex forms or not
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_complexFormEntry, _componentEntry));
        var complexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new() { { "en", "test" } } };
        await Api.CreateComplexFormType(complexFormType);
        await Api.RemoveComplexFormType(_complexFormEntryId, Guid.NewGuid());
    }
}
