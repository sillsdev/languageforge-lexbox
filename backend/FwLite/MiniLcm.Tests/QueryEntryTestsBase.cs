namespace MiniLcm.Tests;

public abstract class QueryEntryTestsBase : MiniLcmTestBase
{
    private string Apple = "Apple";
    private string Peach = "Peach";
    private string Banana = "Banana";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var nounPos = new PartOfSpeech() { Id = Guid.NewGuid(), Name = { { "en", "Noun" } } };
        await Api.CreatePartOfSpeech(nounPos);
        var semanticDomain = new SemanticDomain() { Id = Guid.NewGuid(), Name = { { "en", "Fruit" } }, Code = "1. Fruit"};
        await Api.CreateSemanticDomain(semanticDomain);
        var complexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new(){ { "en", "Very complex" } } };
        await Api.CreateComplexFormType(complexFormType);
        await Api.CreateEntry(new Entry() { LexemeForm = { { "en", Apple } } });
        await Api.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", Peach } },
            ComplexFormTypes = [complexFormType],
            Senses =
            [
                new()
                {
                    Definition = { { "en", "Fruit which tapers to a stem, grows from a tree" } }
                }
            ]
        });
        await Api.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", Banana } },
            Senses =
            [
                new()
                {
                    Gloss = { { "en", "Fruit" } },
                    Definition = { { "en", "Fruit, phone shaped" } },
                    PartOfSpeechId = nounPos.Id,
                    SemanticDomains = [semanticDomain],
                    ExampleSentences =
                    [
                        new ExampleSentence()
                        {
                            Sentence = { { "en", "when a kid hands you a banana phone you answer it" } }
                        }
                    ]
                }
            ]
        });
    }

    [Fact]
    public async Task CanFilterToMissingSenses()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Apple);
    }

    [Fact]
    public async Task CanFilterToMissingPartOfSpeech()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.PartOfSpeechId=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Peach);
    }

    [Fact]
    public async Task CanFilterToMissingExamples()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.ExampleSentences=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo([Apple, Peach]);
    }

    [Fact]
    public async Task CanFilterToMissingSemanticDomains()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.SemanticDomains=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Peach);
    }

    [Fact]
    public async Task CanFilterToMissingComplexFormTypes()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "ComplexFormTypes=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Apple, Banana);
    }

    [Fact]
    public async Task CanFilterLexemeForm()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "LexemeForm[en]=Apple" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Apple);
    }

    [Fact]
    public async Task CanFilterLexemeFormStartsWith()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "LexemeForm[en]^Ap" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Apple);
    }

    [Fact]
    public async Task CanFilterLexemeFormEndsWith()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "LexemeForm[en]$ple" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Apple);
    }

    [Fact]
    public async Task CanFilterLexemeFormContains()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "LexemeForm[en]=*nan" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Banana);
    }

    [Fact]
    public async Task CanFilterGlossNull()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.Gloss[en]=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Peach);
    }

    [Fact]
    public async Task CanFilterGlossEmpty()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.Gloss[en]=" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Peach);
    }
}
