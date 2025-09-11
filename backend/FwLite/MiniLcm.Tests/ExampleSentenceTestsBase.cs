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
}
