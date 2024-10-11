using LcmCrdt.Changes.Entries;
using LcmCrdt.Objects;
using MiniLcm.Models;
using SIL.Harmony.Changes;
using ComplexFormType = MiniLcm.Models.ComplexFormType;

namespace LcmCrdt.Tests.Changes;

public class ComplexFormTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    [Fact]
    public async Task AddComplexFormType()
    {
        var complexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new MultiString() };
        await fixture.Api.CreateComplexFormType(complexFormType);
        var complexEntry = await fixture.Api.CreateEntry(new() { LexemeForm = { { "en", "Coat rack" } }, });
        var change = new AddComplexFormTypeChange(complexEntry.Id,complexFormType);
        await fixture.DataModel.AddChange(Guid.NewGuid(), change);
        complexEntry = await fixture.Api.GetEntry(complexEntry.Id);
        complexEntry.Should().NotBeNull();
        complexEntry!.ComplexFormTypes.Should().ContainSingle().Which.Id.Should().Be(change.ComplexFormType.Id);
    }

    [Fact]
    public async Task RemoveComplexFormType()
    {
        var complexEntry = await fixture.Api.CreateEntry(new() { LexemeForm = { { "en", "Coat rack" } }, });
        var complexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new MultiString() };
        await fixture.Api.CreateComplexFormType(complexFormType);
        await fixture.DataModel.AddChange(
            Guid.NewGuid(),
            new AddComplexFormTypeChange(complexEntry.Id, complexFormType)
        );
        complexEntry = await fixture.Api.GetEntry(complexEntry.Id);
        complexEntry.Should().NotBeNull();
        complexEntry!.ComplexFormTypes.Should().ContainSingle().Which.Id.Should().Be(complexFormType.Id);
        await fixture.DataModel.AddChange(
            Guid.NewGuid(),
            new RemoveComplexFormTypeChange(complexEntry.Id, complexFormType.Id)
        );
        complexEntry = await fixture.Api.GetEntry(complexEntry.Id);
        complexEntry.Should().NotBeNull();
        complexEntry!.ComplexFormTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task AddEntryComponent()
    {
        var complexEntry = await fixture.Api.CreateEntry(new() { LexemeForm = { { "en", "Coat rack" } }, });

        var coatEntry = await fixture.Api.CreateEntry(new() { LexemeForm = { { "en", "Coat" } }, });
        var rackEntry = await fixture.Api.CreateEntry(new() { LexemeForm = { { "en", "Rack" } }, });

        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddEntryComponentChange(ComplexFormComponent.FromEntries(complexEntry, coatEntry)));
        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddEntryComponentChange(ComplexFormComponent.FromEntries(complexEntry, rackEntry)));
        complexEntry = await fixture.Api.GetEntry(complexEntry.Id);
        complexEntry.Should().NotBeNull();
        complexEntry!.Components.Should().ContainSingle(e => e.ComponentEntryId == coatEntry.Id);
        complexEntry.Components.Should().ContainSingle(e => e.ComponentEntryId == rackEntry.Id);

        coatEntry = await fixture.Api.GetEntry(coatEntry.Id);
        coatEntry.Should().NotBeNull();
        coatEntry!.ComplexForms.Should().ContainSingle(e => e.ComplexFormEntryId == complexEntry.Id);
    }

    [Fact]
    public async Task DeleteEntryComponent()
    {
        var complexEntry = await fixture.Api.CreateEntry(new() { LexemeForm = { { "en", "Coat rack" } }, });
        var coatEntry = await fixture.Api.CreateEntry(new() { LexemeForm = { { "en", "Coat" } }, });
        var rackEntry = await fixture.Api.CreateEntry(new() { LexemeForm = { { "en", "Rack" } }, });

        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddEntryComponentChange(ComplexFormComponent.FromEntries(complexEntry, coatEntry)));
        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddEntryComponentChange(ComplexFormComponent.FromEntries(complexEntry, rackEntry)));
        complexEntry = await fixture.Api.GetEntry(complexEntry.Id);
        complexEntry.Should().NotBeNull();
        var component = complexEntry!.Components.First();

        await fixture.DataModel.AddChange(Guid.NewGuid(), new DeleteChange<CrdtComplexFormComponent>(component.Id));
        complexEntry = await fixture.Api.GetEntry(complexEntry.Id);
        complexEntry.Should().NotBeNull();
        complexEntry!.Components.Should().NotContain(c => c.Id == component.Id);
    }
}
