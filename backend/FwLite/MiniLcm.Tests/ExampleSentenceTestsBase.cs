namespace MiniLcm.Tests;

public abstract class ExampleSentenceTestsBase : MiniLcmTestBase
{
    private readonly Guid _entryId = Guid.NewGuid();
    private readonly Guid _senseId = Guid.NewGuid();

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Api.CreateEntry(new Entry()
        {
            Id = _entryId,
            LexemeForm = { { "en", "new-lexeme-form" } },
            Senses =
            [
                new Sense() { Id = _senseId, Gloss = { { "en", "new-sense-gloss" } } }
            ]
        });
    }

    [Fact]
    public async Task CanCreateExampleSentence()
    {
        var expectedExampleSentence = new ExampleSentence()
        {
            SenseId = _senseId,
            Reference = new RichString("This is a reference", "en"),
            Sentence = { { "en", new RichString("test", "en") } },
            Translations = { new Translation() { Text = { { "en", new RichString("test", "en") } } } }
        };
        var actualSentence = await Api.CreateExampleSentence(_entryId, _senseId, expectedExampleSentence);
        actualSentence.Should().BeEquivalentTo(expectedExampleSentence);
    }

    [Fact]
    public async Task CanCreateEmptyExampleSentence()
    {
        var expectedExampleSentence = new ExampleSentence()
        {
            SenseId = _senseId
        };
        var actualSentence = await Api.CreateExampleSentence(_entryId, _senseId, expectedExampleSentence);
        actualSentence.Should().BeEquivalentTo(expectedExampleSentence);
    }

    [Fact]
    public async Task AddTranslation_AddsTranslationToExampleSentence()
    {
        // Arrange: create an example sentence without translations
        var example = await Api.CreateExampleSentence(_entryId, _senseId, new ExampleSentence
        {
            SenseId = _senseId,
            Sentence = { { "en", new RichString("example", "en") } }
        });

        var translation = new Translation
        {
            Id = Guid.NewGuid(),
            Text = { { "en", new RichString("translation", "en") } }
        };

        // Act
        await Api.AddTranslation(_entryId, _senseId, example.Id, translation);

        // Assert
        var updatedExample = await Api.GetExampleSentence(_entryId, _senseId, example.Id);
        updatedExample.Should().NotBeNull();
        updatedExample.Translations.Should().ContainSingle();
        var added = updatedExample.Translations[0];
        added.Id.Should().Be(translation.Id);
        added.Text["en"].Should().BeEquivalentTo(new RichString("translation", "en"));
    }

    [Fact]
    public async Task UpdateTranslation_UpdatesExistingTranslation()
    {
        // Arrange: create an example sentence with one translation
        var exampleId = Guid.NewGuid();
        var translationId = Guid.NewGuid();
        var example = await Api.CreateExampleSentence(_entryId, _senseId, new ExampleSentence
        {
            Id = exampleId,
            SenseId = _senseId,
            Sentence = { { "en", new RichString("example", "en") } },
            Translations =
            [
                new Translation { Id = translationId, Text = { { "en", new RichString("old", "en") } } }
            ]
        });
        example.Translations.Should().ContainSingle();

        // Act
        await Api.UpdateTranslation(_entryId, _senseId, exampleId, translationId,
            new UpdateObjectInput<Translation>().Set(t => t.Text["en"], new RichString("updated", "en")));

        // Assert
        var updatedExample = await Api.GetExampleSentence(_entryId, _senseId, exampleId);
        updatedExample.Should().NotBeNull();
        updatedExample.Translations.Should().ContainSingle();
        var updated = updatedExample.Translations[0];
        updated.Id.Should().Be(translationId);
        updated.Text["en"].Should().BeEquivalentTo(new RichString("updated", "en"));
    }

    [Fact]
    public async Task RemoveTranslation_RemovesExistingTranslation()
    {
        // Arrange: create an example sentence with one translation
        var exampleId = Guid.NewGuid();
        var translationId = Guid.NewGuid();
        var example = await Api.CreateExampleSentence(_entryId, _senseId, new ExampleSentence
        {
            Id = exampleId,
            SenseId = _senseId,
            Sentence = { { "en", new RichString("example", "en") } },
            Translations =
            [
                new Translation { Id = translationId, Text = { { "en", new RichString("to remove", "en") } } }
            ]
        });
        example.Translations.Should().ContainSingle();

        // Act
        await Api.RemoveTranslation(_entryId, _senseId, exampleId, translationId);

        // Assert
        var reloaded = await Api.GetExampleSentence(_entryId, _senseId, exampleId);
        reloaded.Should().NotBeNull();
        reloaded.Translations.Should().BeEmpty();
    }
}
