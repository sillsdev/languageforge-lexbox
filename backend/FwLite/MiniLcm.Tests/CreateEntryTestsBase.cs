using MiniLcm.Tests.AutoFakerHelpers;

namespace MiniLcm.Tests;

public abstract class CreateEntryTestsBase : MiniLcmTestBase
{
    [Fact]
    public async Task CanCreateEntry()
    {
        var entry = await Api.CreateEntry(new() { LexemeForm = { { "en", "test" } } });
        entry.Should().NotBeNull();
        entry.LexemeForm.Values.Should().ContainKey("en");
        entry.LexemeForm["en"].Should().Be("test");
    }

    [Fact]
    public async Task CanCreateEntry_AutoFaker()
    {
        var entry = await AutoFaker.EntryReadyForCreation(Api);
        var createdEntry = await Api.CreateEntry(entry);
        createdEntry.Should().BeEquivalentTo(entry, options => options
            .For(e => e.Components).Exclude(e => e.Id)
            .For(e => e.ComplexForms).Exclude(e => e.Id));
    }

    [Fact]
    public async Task CanCreate_WithComponentsProperty()
    {
        var component = await Api.CreateEntry(new() { LexemeForm = { { "en", "test component" } } });
        var entryId = Guid.NewGuid();
        var entry = await Api.CreateEntry(new()
        {
            Id = entryId,
            LexemeForm = { { "en", "test" } },
            Components =
            [
                new ComplexFormComponent()
                {
                    Id = Guid.NewGuid(),
                    ComponentEntryId = component.Id,
                    ComponentHeadword = component.Headword(),
                    ComplexFormEntryId = entryId,
                    ComplexFormHeadword = "test"
                }
            ]
        });
        entry = await Api.GetEntry(entry.Id);
        entry.Should().NotBeNull();
        entry.Components.Should().ContainSingle(c => c.ComponentEntryId == component.Id);
    }

    [Fact]
    public async Task CanCreate_WithComplexFormsProperty()
    {
        var complexForm = await Api.CreateEntry(new() { LexemeForm = { { "en", "test complex form" } } });
        var entryId = Guid.NewGuid();
        var entry = await Api.CreateEntry(new()
        {
            Id = entryId,
            LexemeForm = { { "en", "test" } },
            ComplexForms =
            [
                new ComplexFormComponent()
                {
                    Id = Guid.NewGuid(),
                    ComponentEntryId = entryId,
                    ComponentHeadword = "test",
                    ComplexFormEntryId = complexForm.Id,
                    ComplexFormHeadword = "test complex form"
                }
            ]
        });
        entry = await Api.GetEntry(entry.Id);
        entry.Should().NotBeNull();
        entry.ComplexForms.Should().ContainSingle(c => c.ComplexFormEntryId == complexForm.Id);
    }

    [Fact]
    public async Task CreateEntry_WithComponentSenseDoesShowOnEntryComplexFormsList()
    {
        var componentSenseId = Guid.NewGuid();
        var component = await Api.CreateEntry(new()
        {
            LexemeForm = { { "en", "test component" } },
            Senses = [new Sense() { Id = componentSenseId, Gloss = { { "en", "test component sense" } } }]
        });
        var complexFormEntryId = Guid.NewGuid();
        await Api.CreateEntry(new()
        {
            Id = complexFormEntryId,
            LexemeForm = { { "en", "test" } },
            Components =
            [
                new ComplexFormComponent()
                {
                    Id = Guid.NewGuid(),
                    ComponentEntryId = component.Id,
                    ComponentHeadword = component.Headword(),
                    ComponentSenseId = componentSenseId,
                    ComplexFormEntryId = complexFormEntryId,
                    ComplexFormHeadword = "test"
                }
            ]
        });

        var entry = await Api.GetEntry(component.Id);
        entry.Should().NotBeNull();
        entry.ComplexForms.Should().ContainSingle().Which.ComponentSenseId.Should().Be(componentSenseId);

        entry = await Api.GetEntry(complexFormEntryId);
        entry.Should().NotBeNull();
        entry.Components.Should().ContainSingle(c =>
            c.ComplexFormEntryId == complexFormEntryId && c.ComponentEntryId == component.Id &&
            c.ComponentSenseId == componentSenseId);
    }

    [Fact]
    public async Task CanCreate_WithComplexFormTypesProperty()
    {
        var complexFormType = await Api.CreateComplexFormType(new()
        {
            Name = new MultiString() { { "en", "test complex form type" } }
        });

        var entry = await Api.CreateEntry(new()
        {
            LexemeForm = { { "en", "test" } }, ComplexFormTypes = [complexFormType]
        });
        entry = await Api.GetEntry(entry.Id);
        entry.Should().NotBeNull();
        entry.ComplexFormTypes.Should().ContainSingle(c => c.Id == complexFormType.Id);
    }

    [Fact]
    public async Task CreateEntry_AutoAssignsHomographNumber_WhenDuplicateHeadword()
    {
        var entry1 = await Api.CreateEntry(new() { LexemeForm = { { "en", "homograph" } } });
        entry1.HomographNumber.Should().Be(0, "single entry should have HomographNumber 0");

        var entry2 = await Api.CreateEntry(new() { LexemeForm = { { "en", "homograph" } } });
        entry2.HomographNumber.Should().Be(2, "second entry should get HomographNumber 2");

        // Re-read entry1 to verify it was updated from 0 to 1
        entry1 = await Api.GetEntry(entry1.Id) ?? throw new NullReferenceException();
        entry1.HomographNumber.Should().Be(1, "first entry should be updated to HomographNumber 1");
    }

    [Fact]
    public async Task CreateEntry_DifferentSecondaryOrder_DoesNotShareHomographNumbers()
    {
        var rootEntry = await Api.CreateEntry(new() { LexemeForm = { { "en", "morphtest" } }, MorphType = MorphTypeKind.Root });
        rootEntry.HomographNumber.Should().Be(0, "lone Root entry should have HomographNumber 0");

        var boundRootEntry = await Api.CreateEntry(new() { LexemeForm = { { "en", "morphtest" } }, MorphType = MorphTypeKind.BoundRoot });
        boundRootEntry.HomographNumber.Should().Be(0, "BoundRoot with different SecondaryOrder should have HomographNumber 0");
    }

    [Fact]
    public async Task CreateEntry_AutoAssignsHomographNumber_WithCitationForm()
    {
        var entry1 = await Api.CreateEntry(new() { LexemeForm = { { "en", "cfLexeme1" } }, CitationForm = { { "en", "cfHomograph" } } });
        entry1.HomographNumber.Should().Be(0, "single entry should have HomographNumber 0");

        var entry2 = await Api.CreateEntry(new() { LexemeForm = { { "en", "cfLexeme2" } }, CitationForm = { { "en", "cfHomograph" } } });
        entry2.HomographNumber.Should().Be(2, "second entry with same CitationForm should get HomographNumber 2");

        entry1 = await Api.GetEntry(entry1.Id) ?? throw new NullReferenceException();
        entry1.HomographNumber.Should().Be(1, "first entry should be updated to HomographNumber 1");
    }

    [Fact]
    public async Task CreateEntry_AutoAssignsHomographNumber_ThreeEntries()
    {
        var entry1 = await Api.CreateEntry(new() { LexemeForm = { { "en", "triple" } } });
        entry1.HomographNumber.Should().Be(0);

        var entry2 = await Api.CreateEntry(new() { LexemeForm = { { "en", "triple" } } });
        entry2.HomographNumber.Should().Be(2);

        var entry3 = await Api.CreateEntry(new() { LexemeForm = { { "en", "triple" } } });
        entry3.HomographNumber.Should().Be(3, "third entry should get HomographNumber 3");

        entry1 = await Api.GetEntry(entry1.Id) ?? throw new NullReferenceException();
        entry1.HomographNumber.Should().Be(1, "first entry should remain HomographNumber 1");
    }

    [Fact]
    public async Task CanCreate_WithRichSpanTag()
    {
        var tag1 = Guid.NewGuid();
        var entry = await Api.CreateEntry(new()
        {
            LexemeForm = { { "en", "test" } },
            LiteralMeaning =
            {
                { "en", new RichString([new RichSpan() { Text = "span", Ws = "en", Tags = [tag1] }]) }
            }
        });
        entry.Should().NotBeNull();
        entry.LiteralMeaning["en"].Should().BeEquivalentTo(new RichString([
            new RichSpan() { Text = "span", Ws = "en", Tags = [tag1] }
        ]));
    }
}
