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
        // AsIs so the round-trip comparison isn't disturbed by the default's auto-added main publication.
        var createdEntry = await Api.CreateEntry(entry, CreateEntryOptions.AsIs);
        createdEntry.Should().BeEquivalentTo(entry, options => options
            .For(e => e.Components).Exclude(e => e.Id)
            .For(e => e.ComplexForms).Exclude(e => e.Id)
            .Excluding(e => e.HomographNumber) // FwData auto-assigns to fix broken CRDT numbering
            .Excluding(member => member.Name == nameof(IOrderable.Order)));
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

    [Fact]
    public async Task CreateEntry_ByDefault_AutoAddsMainPublication()
    {
        var mainPublication = await GetOrCreateMainPublication();

        // No options passed -> the default (CreateEntryOptions.WithMainPublication) applies.
        var entry = await Api.CreateEntry(new Entry { LexemeForm = { { "en", "test" } }, PublishIn = [] });

        entry.PublishIn.Should().ContainSingle().Which.Id.Should().Be(mainPublication.Id);
    }

    [Fact]
    public async Task CreateEntry_AutoAddsMainPublication_WhenEnabled()
    {
        var mainPublication = await GetOrCreateMainPublication();

        var entry = await Api.CreateEntry(new Entry { LexemeForm = { { "en", "test" } }, PublishIn = [] }, CreateEntryOptions.WithMainPublication);

        entry.PublishIn.Should().ContainSingle().Which.Id.Should().Be(mainPublication.Id);
    }

    [Fact]
    public async Task CreateEntry_DoesNotAutoAddMainPublication_WhenDisabled()
    {
        await GetOrCreateMainPublication();

        var entry = await Api.CreateEntry(new Entry { LexemeForm = { { "en", "test" } }, PublishIn = [] }, new CreateEntryOptions(AutoAddMainPublication: false));

        entry.PublishIn.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateEntry_DoesNotDoubleAddMainPublication()
    {
        var mainPublication = await GetOrCreateMainPublication();

        var entry = await Api.CreateEntry(new Entry { LexemeForm = { { "en", "test" } }, PublishIn = [mainPublication] }, CreateEntryOptions.WithMainPublication);

        entry.PublishIn.Should().ContainSingle().Which.Id.Should().Be(mainPublication.Id);
    }
}
