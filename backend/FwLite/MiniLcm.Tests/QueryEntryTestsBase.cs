using System.Text;

namespace MiniLcm.Tests;

public abstract class QueryEntryTestsBase : MiniLcmTestBase
{
    private readonly string Apple = "Apple";
    private readonly string Peach = "Peach";
    private readonly string Banana = "Banana";
    private readonly string Kiwi = "Kiwi";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var nounPos = new PartOfSpeech() { Id = Guid.NewGuid(), Name = { { "en", "Noun" } } };
        await Api.CreatePartOfSpeech(nounPos);
        var semanticDomain = new SemanticDomain() { Id = Guid.NewGuid(), Name = { { "en", "Fruit" } }, Code = "1. Fruit" };
        await Api.CreateSemanticDomain(semanticDomain);
        var complexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new() { { "en", "Very complex" } } };
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
                    Definition = { { "en", new RichString("Fruit which tapers to a stem, grows from a tree") } }
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
                    Definition = { { "en", new RichString("Fruit, phone shaped") } },
                    PartOfSpeechId = nounPos.Id,
                    SemanticDomains = [semanticDomain],
                    ExampleSentences =
                    [
                        new ExampleSentence()
                        {
                            Sentence = { { "en", new RichString("when a kid hands you a banana phone you answer it") } }
                        },
                        new ExampleSentence()
                        {
                            Sentence = { { "en", new RichString("a banana peel can be slippery") } }
                        },
                    ]
                },
                new()
                {
                    Gloss = { { "en", "boat" } },
                    PartOfSpeechId = nounPos.Id,
                    SemanticDomains = [semanticDomain],
                }
            ]
        });
        await Api.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", Kiwi } },
            Senses =
            [
                new()
                {
                    Gloss = { { "en", "Fruit" } },
                    Definition = { { "en", new RichString("Fruit, fuzzy with green flesh") } },
                    PartOfSpeechId = nounPos.Id,
                    SemanticDomains = [semanticDomain],
                    ExampleSentences =
                    [
                        new ExampleSentence()
                        {
                            Sentence = { { "en", new RichString("I like eating Kiwis, they taste good") } }
                        },
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
    public async Task CanFilterToNotMissingSenses()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses!=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Kiwi, Peach, Banana);
    }

    [Fact]
    public async Task CanFilterToMissingPartOfSpeech()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.PartOfSpeechId=null" })).ToArrayAsync();
        //does not include entries with no senses
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Peach);
    }

    [Fact]
    public async Task CanFilterToMissingExamples()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.ExampleSentences=null" })).ToArrayAsync();
        //Senses.ExampleSentences=null matches entries which have senses but no examples
        //it does not include Apple because it has no senses, to include it a filter Senses=null is needed
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Peach, Banana);
    }

    [Fact]
    public async Task CanFilterToMissingSemanticDomains()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.SemanticDomains=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Peach);
    }

    [Fact]
    public async Task CanFilterToMissingSemanticDomainsWithEmptyArray()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.SemanticDomains=[]" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Peach);
    }

    [Fact]
    public async Task CanFilterSemanticDomainCodeContains()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.SemanticDomains.Code=*Fruit" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Banana, Kiwi);
    }

    [Fact]
    public async Task CanFilterToMissingComplexFormTypes()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "ComplexFormTypes=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Apple, Banana, Kiwi);
    }

    [Fact]
    public async Task CanFilterToMissingComplexFormTypesWithEmptyArray()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "ComplexFormTypes=[]" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Apple, Banana, Kiwi);
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

    [Fact]
    public async Task CanFilterGlossEqualsFruit()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.Gloss[en]=Fruit" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Banana, Kiwi);
    }

    [Fact]
    public async Task CanFilterLexemeContainsAAndNoComplexFormTypes()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "LexemeForm[en]=*a/i, ComplexFormTypes=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Apple, Banana);
    }

    [Fact(Skip = "Does not work due to Sentence being a rich string now")]
    public async Task CanFilterExampleSentenceText()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.ExampleSentences.Sentence[en]=*phone" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Banana);
    }

    [Theory]
    [InlineData("a", "a")]
    [InlineData("a", "A")]
    [InlineData("A", "Ã")]
    [InlineData("ap", "apple")]
    [InlineData("ap", "APPLE")]
    [InlineData("ing", "walking")]
    [InlineData("ing", "WALKING")]
    [InlineData("Ãp", "Ãpple")]
    [InlineData("Ãp", "ãpple")]
    [InlineData("ap", "Ãpple")]
    public async Task SuccessfulMatches(string searchTerm, string word)
    {
        word = word.Normalize(NormalizationForm.FormD);
        //should we be normalizing the search term internally?
        searchTerm = searchTerm.Normalize(NormalizationForm.FormD);
        await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = word } });
        var words = await Api.SearchEntries(searchTerm).Select(e => e.LexemeForm["en"]).ToArrayAsync();
        words.Should().Contain(word);
    }

    [Theory]
    [InlineData("a", "b")]
    [InlineData("ab", "b")]
    [InlineData("Ã", "A")] // Accented should not match base
    [InlineData("apple", "orange")] // Completely different words
    [InlineData("É", "È")] // Different accents
    public async Task NegativeMatches(string searchTerm, string word)
    {
        word = word.Normalize(NormalizationForm.FormD);
        //should we be normalizing the search term internally?
        searchTerm = searchTerm.Normalize(NormalizationForm.FormD);
        await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = word } });
        var words = await Api.SearchEntries(searchTerm).Select(e => e.LexemeForm["en"]).ToArrayAsync();
        words.Should().NotContain(word);
    }
}
