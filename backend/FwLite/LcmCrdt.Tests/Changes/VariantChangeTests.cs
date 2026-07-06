using LcmCrdt.Changes.Entries;
using MiniLcm.Models;
using SIL.Harmony.Changes;

namespace LcmCrdt.Tests.Changes;

public class VariantChangeTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    private async Task<(Entry variantEntry, Entry mainEntry)> CreateEntryPair()
    {
        var variantEntry = await fixture.Api.CreateEntry(new() { LexemeForm = { { "en", "colour" } }, });
        var mainEntry = await fixture.Api.CreateEntry(new() { LexemeForm = { { "en", "color" } }, });
        return (variantEntry, mainEntry);
    }

    [Fact]
    public async Task AddVariant()
    {
        var (variantEntry, mainEntry) = await CreateEntryPair();

        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddVariantChange(Variant.FromEntries(variantEntry, mainEntry)));
        var updatedVariantEntry = await fixture.Api.GetEntry(variantEntry.Id);
        updatedVariantEntry.Should().NotBeNull();
        updatedVariantEntry!.VariantOf.Should().ContainSingle(v => v.MainEntryId == mainEntry.Id);

        var updatedMainEntry = await fixture.Api.GetEntry(mainEntry.Id);
        updatedMainEntry.Should().NotBeNull();
        updatedMainEntry!.Variants.Should().ContainSingle(v => v.VariantEntryId == variantEntry.Id);
    }

    [Fact]
    public async Task AddVariant_CarriesTypesAndScalars()
    {
        var (variantEntry, mainEntry) = await CreateEntryPair();
        var variantType = await fixture.Api.CreateVariantType(new VariantType { Id = Guid.NewGuid(), Name = new() { { "en", "spelling" } } });

        var variant = Variant.FromEntries(variantEntry, mainEntry) with
        {
            Types = [variantType],
            HideMinorEntry = true,
            Comment = new() { { "en", new RichString("british spelling") } },
        };
        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddVariantChange(variant));

        var link = (await fixture.Api.GetEntry(variantEntry.Id))!.VariantOf.Should().ContainSingle().Subject;
        link.Types.Should().ContainSingle(t => t.Id == variantType.Id);
        link.HideMinorEntry.Should().BeTrue();
        link.Comment["en"].GetPlainText().Should().Be("british spelling");
    }

    [Fact]
    public async Task AddVariant_SkipsDeletedTypes()
    {
        var (variantEntry, mainEntry) = await CreateEntryPair();
        var variantType = await fixture.Api.CreateVariantType(new VariantType { Id = Guid.NewGuid(), Name = new() { { "en", "doomed" } } });
        await fixture.Api.DeleteVariantType(variantType.Id);

        var variant = Variant.FromEntries(variantEntry, mainEntry) with { Types = [variantType] };
        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddVariantChange(variant));

        var link = (await fixture.Api.GetEntry(variantEntry.Id))!.VariantOf.Should().ContainSingle().Subject;
        link.Types.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteVariant()
    {
        var (variantEntry, mainEntry) = await CreateEntryPair();

        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddVariantChange(Variant.FromEntries(variantEntry, mainEntry)));
        var link = (await fixture.Api.GetEntry(variantEntry.Id))!.VariantOf.Should().ContainSingle().Subject;

        await fixture.DataModel.AddChange(Guid.NewGuid(), new DeleteChange<Variant>(link.Id));
        (await fixture.Api.GetEntry(variantEntry.Id))!.VariantOf.Should().BeEmpty();
        (await fixture.Api.GetEntry(mainEntry.Id))!.Variants.Should().BeEmpty();
    }

    [Fact]
    public async Task DuplicateVariantsAreDeleted()
    {
        var (variantEntry, mainEntry) = await CreateEntryPair();

        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddVariantChange(Variant.FromEntries(variantEntry, mainEntry)));
        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddVariantChange(Variant.FromEntries(variantEntry, mainEntry)));

        var updatedVariantEntry = await fixture.Api.GetEntry(variantEntry.Id);
        updatedVariantEntry.Should().NotBeNull();
        updatedVariantEntry!.VariantOf.Should().ContainSingle(v => v.MainEntryId == mainEntry.Id);

        var updatedMainEntry = await fixture.Api.GetEntry(mainEntry.Id);
        updatedMainEntry.Should().NotBeNull();
        updatedMainEntry!.Variants.Should().ContainSingle(v => v.VariantEntryId == variantEntry.Id);
    }

    [Fact]
    public async Task SelfReferenceVariantIsDeleted()
    {
        var (variantEntry, _) = await CreateEntryPair();

        await fixture.DataModel.AddChange(Guid.NewGuid(),
            new AddVariantChange(new Variant { Id = Guid.NewGuid(), VariantEntryId = variantEntry.Id, MainEntryId = variantEntry.Id }));

        var updated = await fixture.Api.GetEntry(variantEntry.Id);
        updated.Should().NotBeNull();
        updated!.VariantOf.Should().BeEmpty();
        updated.Variants.Should().BeEmpty();
    }

    [Fact]
    public async Task AddVariantType()
    {
        var (variantEntry, mainEntry) = await CreateEntryPair();
        var variantType = await fixture.Api.CreateVariantType(new VariantType { Id = Guid.NewGuid(), Name = new() { { "en", "test" } } });

        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddVariantChange(Variant.FromEntries(variantEntry, mainEntry)));
        var link = (await fixture.Api.GetEntry(variantEntry.Id))!.VariantOf.Single();

        var change = new AddVariantTypeChange(link.Id, variantType);
        await fixture.DataModel.AddChange(Guid.NewGuid(), change);

        link = (await fixture.Api.GetEntry(variantEntry.Id))!.VariantOf.Single();
        link.Types.Should().ContainSingle().Which.Id.Should().Be(variantType.Id);
    }

    [Fact]
    public async Task RemoveVariantType()
    {
        var (variantEntry, mainEntry) = await CreateEntryPair();
        var variantType = await fixture.Api.CreateVariantType(new VariantType { Id = Guid.NewGuid(), Name = new() { { "en", "test" } } });

        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddVariantChange(Variant.FromEntries(variantEntry, mainEntry) with { Types = [variantType] }));
        var link = (await fixture.Api.GetEntry(variantEntry.Id))!.VariantOf.Single();
        link.Types.Should().ContainSingle();

        await fixture.DataModel.AddChange(Guid.NewGuid(), new RemoveVariantTypeChange(link.Id, variantType.Id));

        link = (await fixture.Api.GetEntry(variantEntry.Id))!.VariantOf.Single();
        link.Types.Should().BeEmpty();
    }

    [Fact]
    public async Task SetVariantTypesOrder_ReordersTypesAndMergesWithConcurrentAdd()
    {
        var (variantEntry, mainEntry) = await CreateEntryPair();
        var typeA = await fixture.Api.CreateVariantType(new VariantType { Id = Guid.NewGuid(), Name = new() { { "en", "a" } } });
        var typeB = await fixture.Api.CreateVariantType(new VariantType { Id = Guid.NewGuid(), Name = new() { { "en", "b" } } });
        var typeC = await fixture.Api.CreateVariantType(new VariantType { Id = Guid.NewGuid(), Name = new() { { "en", "c" } } });

        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddVariantChange(Variant.FromEntries(variantEntry, mainEntry) with { Types = [typeA, typeB] }));
        var link = (await fixture.Api.GetEntry(variantEntry.Id))!.VariantOf.Single();

        await fixture.DataModel.AddChange(Guid.NewGuid(), new SetVariantTypesOrderChange(link.Id, [typeB.Id, typeA.Id]));
        link = (await fixture.Api.GetEntry(variantEntry.Id))!.VariantOf.Single();
        link.Types.Select(t => t.Id).Should().Equal(typeB.Id, typeA.Id);

        // a type the reorder never listed (e.g. added concurrently) lands after the listed ones
        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddVariantTypeChange(link.Id, typeC));
        await fixture.DataModel.AddChange(Guid.NewGuid(), new SetVariantTypesOrderChange(link.Id, [typeA.Id, typeB.Id]));
        link = (await fixture.Api.GetEntry(variantEntry.Id))!.VariantOf.Single();
        link.Types.Select(t => t.Id).Should().Equal(typeA.Id, typeB.Id, typeC.Id);
    }

    [Fact]
    public async Task DeletingVariantTypeRemovesItFromLinks()
    {
        var (variantEntry, mainEntry) = await CreateEntryPair();
        var variantType = await fixture.Api.CreateVariantType(new VariantType { Id = Guid.NewGuid(), Name = new() { { "en", "test" } } });

        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddVariantChange(Variant.FromEntries(variantEntry, mainEntry) with { Types = [variantType] }));
        await fixture.DataModel.AddChange(Guid.NewGuid(), new DeleteChange<VariantType>(variantType.Id));

        var link = (await fixture.Api.GetEntry(variantEntry.Id))!.VariantOf.Should().ContainSingle().Subject;
        link.DeletedAt.Should().BeNull("deleting a type must not delete the link");
        link.Types.Should().BeEmpty();
    }

    [Fact]
    public async Task DeletingMainEntryDeletesTheLink()
    {
        var (variantEntry, mainEntry) = await CreateEntryPair();
        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddVariantChange(Variant.FromEntries(variantEntry, mainEntry)));

        await fixture.DataModel.AddChange(Guid.NewGuid(), new DeleteChange<Entry>(mainEntry.Id));

        var updatedVariantEntry = await fixture.Api.GetEntry(variantEntry.Id);
        updatedVariantEntry.Should().NotBeNull("only the link dies, not the variant entry");
        updatedVariantEntry!.VariantOf.Should().BeEmpty();
    }

    [Fact]
    public async Task DeletingVariantEntryDeletesTheLink()
    {
        var (variantEntry, mainEntry) = await CreateEntryPair();
        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddVariantChange(Variant.FromEntries(variantEntry, mainEntry)));

        await fixture.DataModel.AddChange(Guid.NewGuid(), new DeleteChange<Entry>(variantEntry.Id));

        var updatedMainEntry = await fixture.Api.GetEntry(mainEntry.Id);
        updatedMainEntry.Should().NotBeNull("only the link dies, not the main entry");
        updatedMainEntry!.Variants.Should().BeEmpty();
    }

    [Fact]
    public async Task DeletingTargetSenseDeletesTheLink()
    {
        var (variantEntry, mainEntry) = await CreateEntryPair();
        var sense = await fixture.Api.CreateSense(mainEntry.Id, new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "target" } } });
        await fixture.DataModel.AddChange(Guid.NewGuid(), new AddVariantChange(Variant.FromEntries(variantEntry, mainEntry, sense.Id)));

        await fixture.Api.DeleteSense(mainEntry.Id, sense.Id);

        (await fixture.Api.GetEntry(variantEntry.Id))!.VariantOf.Should().BeEmpty();
        (await fixture.Api.GetEntry(mainEntry.Id))!.Variants.Should().BeEmpty();
    }
}
