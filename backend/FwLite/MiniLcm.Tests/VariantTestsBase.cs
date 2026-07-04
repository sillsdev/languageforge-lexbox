using MiniLcm.Models;

namespace MiniLcm.Tests;

public abstract class VariantTestsBase : MiniLcmTestBase
{
    protected readonly Guid _mainEntryId = Guid.NewGuid();
    protected readonly Guid _variantEntryId = Guid.NewGuid();
    protected readonly Guid _mainSenseId1 = Guid.NewGuid();
    protected readonly Guid _mainSenseId2 = Guid.NewGuid();
    protected Entry _mainEntry = null!;
    protected Entry _variantEntry = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _mainEntry = await Api.CreateEntry(new()
        {
            Id = _mainEntryId,
            LexemeForm = { { "en", "main entry" } },
            Senses =
            [
                new Sense
                {
                    Id = _mainSenseId1,
                    Gloss = { { "en", "main sense 1" } }
                },
                new Sense
                {
                    Id = _mainSenseId2,
                    Gloss = { { "en", "main sense 2" } }
                }
            ]
        });
        // deliberately sense-less: FLEx's "Insert Variant" creates variant entries without senses
        _variantEntry = await Api.CreateEntry(new()
        {
            Id = _variantEntryId,
            LexemeForm = { { "en", "variant form" } }
        });
    }

    private async Task<VariantType> CreateVariantType(string name = "test type")
    {
        return await Api.CreateVariantType(new VariantType { Id = Guid.NewGuid(), Name = new() { { "en", name } } });
    }

    [Fact]
    public async Task CreateVariant_Works()
    {
        var variant = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        variant.VariantEntryId.Should().Be(_variantEntryId);
        variant.MainEntryId.Should().Be(_mainEntryId);
        variant.MainSenseId.Should().BeNull();
        variant.VariantHeadword.Should().Be("variant form");
        variant.MainHeadword.Should().Be("main entry");
        variant.Types.Should().BeEmpty();
        variant.HideMinorEntry.Should().BeFalse();
        variant.Comment.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateVariant_WithSense_Works()
    {
        var variant = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry, _mainSenseId1));
        variant.MainSenseId.Should().Be(_mainSenseId1);

        // a sense-targeted link still surfaces under the owning entry's Variants
        var mainEntry = await Api.GetEntry(_mainEntryId);
        mainEntry!.Variants.Should().ContainSingle(v => v.VariantEntryId == _variantEntryId && v.MainSenseId == _mainSenseId1);
    }

    [Fact]
    public async Task CreateVariant_WithTypesCommentAndHideMinorEntry_RoundTrips()
    {
        var type = await CreateVariantType();
        var input = Variant.FromEntries(_variantEntry, _mainEntry) with
        {
            Types = [type],
            HideMinorEntry = true,
            Comment = new() { { "en", new RichString("originally meant something else") } },
        };
        await Api.CreateVariant(input);

        var variantEntry = await Api.GetEntry(_variantEntryId);
        var link = variantEntry!.VariantOf.Should().ContainSingle().Subject;
        link.Types.Should().ContainSingle(t => t.Id == type.Id);
        link.HideMinorEntry.Should().BeTrue();
        link.Comment["en"].GetPlainText().Should().Be("originally meant something else");

        var mainEntry = await Api.GetEntry(_mainEntryId);
        var backLink = mainEntry!.Variants.Should().ContainSingle().Subject;
        backLink.Types.Should().ContainSingle(t => t.Id == type.Id);
        backLink.HideMinorEntry.Should().BeTrue();
        backLink.Comment["en"].GetPlainText().Should().Be("originally meant something else");
    }

    [Fact]
    public async Task VariantHeadwords_UpdateWhenReferencedEntriesChange()
    {
        await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));

        var beforeVariant = _variantEntry.Copy();
        _variantEntry.LexemeForm["en"] = "renamed variant";
        await Api.UpdateEntry(beforeVariant, _variantEntry);

        var mainEntry = (await Api.GetEntry(_mainEntryId))!;
        var beforeMain = mainEntry.Copy();
        mainEntry.LexemeForm["en"] = "renamed main";
        await Api.UpdateEntry(beforeMain, mainEntry);

        var link = (await Api.GetEntry(_mainEntryId))!.Variants.Should().ContainSingle().Subject;
        link.VariantHeadword.Should().Be("renamed variant");
        link.MainHeadword.Should().Be("renamed main");
    }

    [Fact]
    public async Task DeleteVariant_Works()
    {
        var variant = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        await Api.DeleteVariant(variant);
        var entries = await Api.GetEntries().ToArrayAsync();
        var variantEntry = entries.Should().ContainSingle(e => e.Id == _variantEntryId).Subject;
        var mainEntry = entries.Should().ContainSingle(e => e.Id == _mainEntryId).Subject;
        variantEntry.VariantOf.Should().BeEmpty();
        mainEntry.Variants.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEntries_Works()
    {
        await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        var entries = await Api.GetEntries().ToArrayAsync();
        var variantEntry = entries.Should().ContainSingle(e => e.Id == _variantEntryId).Subject;
        var mainEntry = entries.Should().ContainSingle(e => e.Id == _mainEntryId).Subject;
        variantEntry.VariantOf.Should().ContainSingle(v => v.MainEntryId == _mainEntryId);
        mainEntry.Variants.Should().ContainSingle(v => v.VariantEntryId == _variantEntryId);
    }

    [Fact]
    public async Task VariantEntryWithoutSenses_RoundTrips()
    {
        await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        var variantEntry = await Api.GetEntry(_variantEntryId);
        variantEntry.Should().NotBeNull();
        variantEntry!.Senses.Should().BeEmpty();
        variantEntry.VariantOf.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateVariant_UsingTheSameLinkDoesNothing()
    {
        var variant1 = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        var variant2 = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        variant2.Should().BeEquivalentTo(variant1, options => options.ComparingByMembers<Variant>().Excluding(v => v.Id));
        (await Api.GetEntry(_variantEntryId))!.VariantOf.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateVariant_UsingTheSameLinkWithSenseDoesNothing()
    {
        var variant1 = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry, _mainSenseId1));
        var variant2 = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry, _mainSenseId1));
        variant2.Should().BeEquivalentTo(variant1, options => options.ComparingByMembers<Variant>().Excluding(v => v.Id));
        (await Api.GetEntry(_variantEntryId))!.VariantOf.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateVariant_ReplayingReturnedObject_IsIdempotent()
    {
        // Sync can be interrupted and replayed, so the exact same object (including its
        // internal entity ID) may be passed to CreateVariant again.
        var created = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        var again = await Api.CreateVariant(created);
        again.Should().BeEquivalentTo(created, options => options.ComparingByMembers<Variant>().Excluding(v => v.Id));
    }

    [Fact]
    public async Task CreateVariant_CanTargetMultipleSensesOfSameEntry()
    {
        var variant1 = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry, _mainSenseId1));
        variant1.MainSenseId.Should().Be(_mainSenseId1);
        var variant2 = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry, _mainSenseId2));
        variant2.MainSenseId.Should().Be(_mainSenseId2);

        // ensure our sync code can handle them too
        _variantEntry = (await Api.GetEntry(_variantEntryId))!;
        await Api.UpdateEntry(_variantEntry, _variantEntry);
        _mainEntry = (await Api.GetEntry(_mainEntryId))!;
        await Api.UpdateEntry(_mainEntry, _mainEntry);
    }

    [Fact]
    public async Task CreateVariant_ChangingPropertyAndCreatingAgain_CreatesBoth()
    {
        var newMainEntry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "new main" } }
        });

        var input = Variant.FromEntries(_variantEntry, _mainEntry);
        var first = await Api.CreateVariant(input);
        first.MainEntryId.Should().Be(_mainEntryId);

        // Mutate a property on the same object and create again.
        // The sync diff does this when a property changes (remove + add).
        input.MainEntryId = newMainEntry.Id;
        var second = await Api.CreateVariant(input);
        second.MainEntryId.Should().Be(newMainEntry.Id);

        var entry = await Api.GetEntry(_variantEntryId);
        entry!.VariantOf.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateVariant_ThrowsOnSelfReference()
    {
        var act = async () => await Api.CreateVariant(Variant.FromEntries(_mainEntry, _mainEntry));
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateVariant_AllowsChains()
    {
        // FLEx allows variant chains (a variant of a variant); we must too
        var entry3 = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "entry3" } }
        });
        await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        await Api.CreateVariant(Variant.FromEntries(entry3, _variantEntry));
        var middle = await Api.GetEntry(_variantEntryId);
        middle!.VariantOf.Should().ContainSingle(v => v.MainEntryId == _mainEntryId);
        middle.Variants.Should().ContainSingle(v => v.VariantEntryId == entry3.Id);
    }

    [Fact]
    public async Task CreateVariant_ThrowsWhenMakingA2LayerReferenceCycle()
    {
        // LCM rejects circular component references (LexEntryRef.ValidateAddObjectInternal),
        // so both implementations must too
        await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        var act = async () => await Api.CreateVariant(Variant.FromEntries(_mainEntry, _variantEntry));
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateVariant_ThrowsWhenMakingA3LayerReferenceCycle()
    {
        var entry3 = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "entry3" } }
        });
        await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        await Api.CreateVariant(Variant.FromEntries(_mainEntry, entry3));
        var act = async () => await Api.CreateVariant(Variant.FromEntries(entry3, _variantEntry));
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateVariant_ThrowsWhenClosingAMixedCycleThroughAComplexForm()
    {
        // LCM's cycle check spans the combined complex-form + variant component graph
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(_mainEntry, _variantEntry));
        var act = async () => await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateVariant_WorksWhenALinkWasDeletedWhichWouldCauseACycle()
    {
        var created = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        await Api.DeleteVariant(created);
        var act = async () => await Api.CreateVariant(Variant.FromEntries(_mainEntry, _variantEntry));
        await act.Should().NotThrowAsync("a link was deleted which was part of the cycle");
    }

    [Fact]
    public async Task UpdateVariant_SyncsTypesAndScalars()
    {
        var typeA = await CreateVariantType("type a");
        var typeB = await CreateVariantType("type b");
        var created = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry) with { Types = [typeA] });

        var after = created.Copy();
        after.Types = [typeB];
        after.HideMinorEntry = true;
        after.Comment = new() { { "en", new RichString("now hidden") } };
        await Api.UpdateVariant(created, after);

        var link = (await Api.GetEntry(_variantEntryId))!.VariantOf.Should().ContainSingle().Subject;
        link.Types.Should().ContainSingle(t => t.Id == typeB.Id);
        link.HideMinorEntry.Should().BeTrue();
        link.Comment["en"].GetPlainText().Should().Be("now hidden");
    }

    [Fact]
    public async Task AddVariantType_Works()
    {
        var type = await CreateVariantType();
        var variant = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        await Api.AddVariantType(variant, type.Id);
        var link = (await Api.GetEntry(_variantEntryId))!.VariantOf.Should().ContainSingle().Subject;
        link.Types.Should().ContainSingle(t => t.Id == type.Id);
    }

    [Fact]
    public async Task AddVariantType_IsIdempotent()
    {
        var type = await CreateVariantType();
        var variant = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        await Api.AddVariantType(variant, type.Id);
        await Api.AddVariantType(variant, type.Id);
        var link = (await Api.GetEntry(_variantEntryId))!.VariantOf.Should().ContainSingle().Subject;
        link.Types.Should().ContainSingle(t => t.Id == type.Id);
    }

    [Fact]
    public async Task RemoveVariantType_Works()
    {
        var type = await CreateVariantType();
        var variant = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry) with { Types = [type] });
        await Api.RemoveVariantType(variant, type.Id);
        var link = (await Api.GetEntry(_variantEntryId))!.VariantOf.Should().ContainSingle().Subject;
        link.Types.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveVariantType_WorksWhenTypeIsNotOnLink()
    {
        var variant = await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        await Api.RemoveVariantType(variant, Guid.NewGuid());
    }

    [Fact]
    public async Task CreateVariantType_Works()
    {
        var variantType = new VariantType() { Id = Guid.NewGuid(), Name = new() { { "en", "test" } } };
        await Api.CreateVariantType(variantType);
        var types = await Api.GetVariantTypes().ToArrayAsync();
        types.Should().ContainSingle(t => t.Id == variantType.Id);
    }

    [Fact]
    public async Task UpdateVariantType_Works()
    {
        var variantType = new VariantType() { Id = Guid.NewGuid(), Name = new() { { "en", "test" } } };
        await Api.CreateVariantType(variantType);
        var updatedVariantType = await Api.UpdateVariantType(variantType.Id, new UpdateObjectInput<VariantType>().Set(c => c.Name["en"], "updated"));
        updatedVariantType.Name["en"].Should().Be("updated");
    }

    [Fact]
    public async Task UpdateVariantTypeSync_Works()
    {
        var variantType = new VariantType() { Id = Guid.NewGuid(), Name = new() { { "en", "test" } } };
        await Api.CreateVariantType(variantType);
        var afterVariantType = variantType with { Name = new() { { "en", "updated" } } };
        var actualVariantType = await Api.UpdateVariantType(variantType, afterVariantType);
        actualVariantType.Should().BeEquivalentTo(afterVariantType, options => options.Excluding(c => c.Id));
    }

    [Fact]
    public async Task EntryCanBeBothVariantAndComponent()
    {
        var complexFormEntry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "complex form" } }
        });
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(complexFormEntry, _variantEntry));
        await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));

        var entry = await Api.GetEntry(_variantEntryId);
        entry!.ComplexForms.Should().ContainSingle(c => c.ComplexFormEntryId == complexFormEntry.Id);
        entry.VariantOf.Should().ContainSingle(v => v.MainEntryId == _mainEntryId);
    }
}
