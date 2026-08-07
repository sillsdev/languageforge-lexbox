using LcmCrdt.Data;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;

namespace LcmCrdt.Tests.Data;

public class QueryHelperTests: IAsyncLifetime
{
    private readonly MiniLcmApiFixture _fixture = new();
    private IMiniLcmApi Api => _fixture.Api;

    public Task InitializeAsync()
    {
        return _fixture.InitializeAsync();
    }
    public Task DisposeAsync()
    {
        return _fixture.DisposeAsync();
    }

    [Fact]
    public async Task QueryEntryMorphType()
    {
        var pluralEntry = await Api.CreateEntry(new Entry() { LexemeForm = { { "en", "s" } }, MorphType = MorphTypeKind.Suffix, });
        var morphType = await _fixture.DbContext.Entries.Where(e => e.Id == pluralEntry.Id)
            .Select(e => e.QueryMorphType())
            .SingleAsyncLinqToDB();
        morphType.Should().NotBeNull();
        morphType.Kind.Should().Be(MorphTypeKind.Suffix);
    }

    [Fact]
    public async Task QueryEntryHeadword()
    {
        var pluralEntry = await Api.CreateEntry(new Entry() { LexemeForm = { { "en", "s" } }, MorphType = MorphTypeKind.Suffix, });
        (await _fixture.DbContext.Entries.Where(e => e.Id == pluralEntry.Id).Select(e => e.QueryHeadwordWithTokens(EntryQueryHelpers.DefaultWritingSystem(WritingSystemType.Vernacular)))
            .SingleAsyncLinqToDB())
            .Should().Be("-s");

        var appleEntry =  await Api.CreateEntry(new Entry() { LexemeForm = { { "en", "apple" } }, MorphType = MorphTypeKind.Stem });
        (await _fixture.DbContext.Entries.Where(e => e.Id == appleEntry.Id)
                .Select(e => e.QueryHeadwordWithTokens(EntryQueryHelpers.DefaultWritingSystem(WritingSystemType.Vernacular)))
            .SingleAsyncLinqToDB())
            .Should().Be("apple");
    }

    [Fact]
    public async Task QueryComplexFormComponentEntry()
    {
        var appleEntry = await Api.CreateEntry(new Entry() { LexemeForm = { { "en", "apple" } }, });
        var pluralEntry = await Api.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", "s" } }, MorphType = MorphTypeKind.Suffix,
        });
        var applesEntry = await Api.CreateEntry(new Entry() { LexemeForm = { { "en", "apples" } }, });
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(applesEntry, appleEntry));
        var pluralComponent = await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(applesEntry, pluralEntry));
        var componentEntry = await _fixture.DbContext.ComplexFormComponents
                .Where(p => p.Id == pluralComponent.Id)
                .Select(p => EntryQueryHelpers.QueryComponentEntry(p))
                .SingleAsyncLinqToDB();

        componentEntry.Should().BeEquivalentTo(pluralEntry);

        var complexEntry = await _fixture.DbContext.ComplexFormComponents
                .Where(p => p.Id == pluralComponent.Id)
                .Select(p => EntryQueryHelpers.QueryComplexFormEntry(p))
                .SingleAsyncLinqToDB();

        complexEntry.Should().BeEquivalentTo(applesEntry);
    }

    [Fact]
    public async Task QueryComponentHeadword()
    {
        var appleEntry =
            await Api.CreateEntry(new Entry() { LexemeForm = { { "en", "apple" } }, MorphType = MorphTypeKind.Stem });
        var pluralEntry = await Api.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", "s" } }, MorphType = MorphTypeKind.Suffix,
        });
        var complexEntry = await Api.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", "apples" } },
            MorphType = MorphTypeKind.Stem
        });

        var appleComponent = await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(complexEntry, appleEntry));
        var appleComponentFromDb = await _fixture.DbContext.ComplexFormComponents.SingleAsyncLinqToDB(p => p.Id == appleComponent.Id);
        appleComponentFromDb.ComponentHeadword.Should().Be("apple");
        appleComponentFromDb.ComplexFormHeadword.Should().Be("apples");

        var pluralComponent = await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(complexEntry, pluralEntry));
        var pluralComponentFromDb = await  _fixture.DbContext.ComplexFormComponents.SingleAsyncLinqToDB(p => p.Id == pluralComponent.Id);
        pluralComponentFromDb.ComponentHeadword.Should().Be("-s");
        pluralComponentFromDb.ComplexFormHeadword.Should().Be("apples");

        var apples = await _fixture.DbContext.Entries
            .LoadWith(e => e.ComplexForms)
            .LoadWith(e => e.Components)
            .SingleOrDefaultAsyncLinqToDB(e => e.Id == complexEntry.Id);

        apples.Should().NotBeNull();
        apples.Components.Should().NotBeNull();
        apples.Components.Should().HaveCount(2);

        apples.Components[0].ComponentHeadword.Should().Be("apple");
        apples.Components[1].ComponentHeadword.Should().Be("-s");
    }
}
