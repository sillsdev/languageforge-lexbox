using MiniLcm.Models;

namespace MiniLcm.Tests;

public abstract class ComplexFormComponentTestsBase : MiniLcmTestBase
{
    private readonly Guid _complexFormEntryId = Guid.NewGuid();
    private readonly Guid _componentEntryId = Guid.NewGuid();
    private readonly Guid _componentSenseId1 = Guid.NewGuid();
    private readonly Guid _componentSenseId2 = Guid.NewGuid();
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
    public async Task CreateComplexFormType_Works()
    {
        var complexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new() { { "en", "test" } } };
        await Api.CreateComplexFormType(complexFormType);
        var types = await Api.GetComplexFormTypes().ToArrayAsync();
        types.Should().ContainSingle(t => t.Id == complexFormType.Id);
    }
}
