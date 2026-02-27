using System.Text;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;

namespace MiniLcm.Tests;

public abstract class QueryEntryTestsBase : MiniLcmTestBase
{
    private readonly Guid appleId = Guid.NewGuid();
    private readonly string Apple = "Apple";
    private readonly string Peach = "Peach";
    private readonly string Banana = "Banana";
    private readonly string Kiwi = "Kiwi";
    private readonly string Null_LexemeForm = string.Empty; // nulls get normalized to empty strings
    private Publication mainDictionary = null!;

    private static readonly AutoFaker Faker = new(AutoFakerDefault.Config);

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var nounPos = new PartOfSpeech() { Id = Guid.NewGuid(), Name = { { "en", "Noun" } } };
        await Api.CreatePartOfSpeech(nounPos);
        var semanticDomain = new SemanticDomain() { Id = Guid.NewGuid(), Name = { { "en", "Fruit" } }, Code = "1. Fruit" };
        await Api.CreateSemanticDomain(semanticDomain);
        var complexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new() { { "en", "Very complex" } } };
        await Api.CreateComplexFormType(complexFormType);
        mainDictionary = new Publication() { Id = Guid.NewGuid(), Name = { { "en", "Main Dictionary" } } };
        await Api.CreatePublication(mainDictionary);
        await Api.CreateEntry(new Entry()
        {
            Id = appleId,
            LexemeForm = { { "en", Apple } },
            MorphType = MorphType.Root,
        });
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
            PublishIn = [mainDictionary],
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
            PublishIn = [mainDictionary],
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
        // null / missing key - exposes potential NPEs
        await Api.CreateEntry(new Entry());
    }

    [Fact]
    public async Task Get_MissingEntry_ReturnsNull()
    {
        var entry = await Api.GetEntry(Guid.NewGuid());
        entry.Should().BeNull();
    }

    [Fact]
    public async Task Get_ExistingEntry_ReturnsEntry()
    {
        var entry = await Api.GetEntry(appleId);
        entry.Should().NotBeNull();
        entry.LexemeForm["en"].Should().Be(Apple);
    }

    [Fact]
    public async Task CanFilterToMissingSenses()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses=null" })).ToArrayAsync();
        //using distinct since there may be 2 null lexeme forms but only on FLEx due to the null lexeme form
        results.Select(e => e.LexemeForm["en"]).Distinct().Should().BeEquivalentTo(Apple, Null_LexemeForm);
    }

    [Fact]
    public async Task CanFilterToMissingSenses_AndSearch()
    {
        var results = await Api.SearchEntries(Apple, new(Filter: new() { GridifyFilter = "Senses=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Apple);
    }

    [Fact]
    public async Task CanFilterToNotMissingSenses()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses!=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Kiwi, Peach, Banana);
    }

    [Fact]
    public async Task CanFilterToNotMissingSenses_AndSearch()
    {
        var results = await Api.SearchEntries(Banana, new(Filter: new() { GridifyFilter = "Senses!=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Banana);
    }

    [Fact]
    public async Task CanFilterToMissingPartOfSpeech()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.PartOfSpeechId=" })).ToArrayAsync();
        //does not include entries with no senses
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Peach);
    }

    [Fact]
    public async Task CanFilterToMissingPartOfSpeech_AndSearch()
    {
        var results = await Api.SearchEntries(Peach, new(Filter: new() { GridifyFilter = "Senses.PartOfSpeechId=" })).ToArrayAsync();
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
    public async Task CanFilterToMissingExamples_AndSearch()
    {
        var results = await Api.SearchEntries(Banana, new(Filter: new() { GridifyFilter = "Senses.ExampleSentences=null" })).ToArrayAsync();
        //Senses.ExampleSentences=null matches entries which have senses but no examples
        //it does not include Apple because it has no senses, to include it a filter Senses=null is needed
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Banana);
    }

    [Fact]
    public async Task CanFilterToMissingSemanticDomains()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.SemanticDomains=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Peach);
    }

    [Fact]
    public async Task CanFilterToMissingSemanticDomains_AndSearch()
    {
        var results = await Api.SearchEntries(Peach, new(Filter: new() { GridifyFilter = "Senses.SemanticDomains=null" })).ToArrayAsync();
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
        //using distinct since there may be 2 null lexeme forms but only on FLEx due to the null lexeme form
        results.Select(e => e.LexemeForm["en"]).Distinct().Should().BeEquivalentTo(Apple, Banana, Kiwi, Null_LexemeForm);
    }

    [Fact]
    public async Task CanFilterToMissingComplexFormTypes_AndSearch()
    {
        var results = await Api.SearchEntries(Banana, new(Filter: new() { GridifyFilter = "ComplexFormTypes=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Banana);
    }

    [Fact]
    public async Task CanFilterToMissingComplexFormTypesWithEmptyArray()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "ComplexFormTypes=[]" })).ToArrayAsync();
        //using distinct since there may be 2 null lexeme forms but only on FLEx due to the null lexeme form
        results.Select(e => e.LexemeForm["en"]).Distinct().Should().BeEquivalentTo(Apple, Banana, Kiwi, Null_LexemeForm);
    }

    [Fact]
    public async Task CanFilterToMissingPublishIn()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "PublishIn=null" })).ToArrayAsync();
        //Apple, Peach, and the null entry have no publications
        results.Select(e => e.LexemeForm["en"]).Distinct().Should().BeEquivalentTo(Apple, Peach, Null_LexemeForm);
    }

    [Fact]
    public async Task CanFilterToMissingPublishIn_AndSearch()
    {
        var results = await Api.SearchEntries(Apple, new(Filter: new() { GridifyFilter = "PublishIn=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Apple);
    }

    [Fact]
    public async Task CanFilterToMissingPublishInWithEmptyArray()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "PublishIn=[]" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Distinct().Should().BeEquivalentTo(Apple, Peach, Null_LexemeForm);
    }

    [Fact]
    public async Task CanFilterByPublicationId()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = $"PublishIn.Id={mainDictionary.Id}" })).ToArrayAsync();
        //Banana and Kiwi are in the main dictionary
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Banana, Kiwi);
    }

    [Fact]
    public async Task CanFilterByPublicationId_AndSearch()
    {
        var results = await Api.SearchEntries(Banana, new(Filter: new() { GridifyFilter = $"PublishIn.Id={mainDictionary.Id}" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Banana);
    }

    [Fact]
    public async Task CanFilterByMorphTypeSingleType()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "MorphType=Root" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Apple);
    }

    [Fact]
    public async Task CanFilterByMorphTypeTwoTypes()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "MorphType=Root|MorphType=Stem" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Apple, Peach, Banana, Kiwi, Null_LexemeForm);
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
    public async Task CanFilterLexemeFormContains_AndSearch()
    {
        var results = await Api.SearchEntries(Banana, new(Filter: new() { GridifyFilter = "LexemeForm[en]=*nan" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Banana);
    }

    [Fact]
    public async Task CanFilterGlossNull()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.Gloss[en]=null" })).ToArrayAsync();
        /// No entries have a gloss of "null"
        /// <see cref="Filtering.EntryFilter.NewMapper"/> and <see cref="NullAndEmptyQueryEntryTestsBase"/>
        results.Select(e => e.LexemeForm["en"]).Should().BeEmpty();
    }

    [Fact]
    public async Task CanFilterGlossEmptyOrNull()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.Gloss[en]=" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Peach);
    }

    [Fact]
    public async Task CanFilterGlossEmptyOrNull_AndSearch()
    {
        var results = await Api.SearchEntries(Peach, new(Filter: new() { GridifyFilter = "Senses.Gloss[en]=" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Peach);
    }

    [Fact]
    public async Task CanFilterGlossEqualsFruit()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.Gloss[en]=Fruit" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Banana, Kiwi);
    }

    [Fact]
    public async Task CanFilterGlossEqualsFruit_AndSearch()
    {
        var results = await Api.SearchEntries(Banana, new(Filter: new() { GridifyFilter = "Senses.Gloss[en]=Fruit" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Banana);
    }

    [Fact]
    public async Task CanFilterLexemeContainsAAndNoComplexFormTypes()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "LexemeForm[en]=*a/i, ComplexFormTypes=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Apple, Banana);
    }

    [Fact]
    public async Task CanFilterExampleSentenceText()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "Senses.ExampleSentences.Sentence[en]=*phone" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Banana);
    }

    [Fact]
    public async Task CanFilterExampleSentenceText_AndSearch()
    {
        var results = await Api.SearchEntries(Banana, new(Filter: new() { GridifyFilter = "Senses.ExampleSentences.Sentence[en]=*phone" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Banana);
    }

    [Fact]
    public async Task CanFilterToExampleSentenceWithMissingSentence()
    {
        var results = await Api
            .GetEntries(new(Filter: new() { GridifyFilter = "Senses.ExampleSentences.Sentence[es]=" })).ToArrayAsync();
        //Senses.ExampleSentences=null matches entries which have senses but no examples
        //it does not include Apple because it has no senses, to include it a filter Senses=null is needed
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Kiwi, Banana);
    }

    [Theory]
    [InlineData("a", "a", true)]
    [InlineData("a", "A", false)]
    [InlineData("A", "Ã", false)]
    [InlineData("ap", "apple", false)]
    [InlineData("ap", "APPLE", false)]
    [InlineData("ing", "walking", false)]
    [InlineData("ing", "WALKING", false)]
    [InlineData("Ãp", "Ãpple", false)]
    [InlineData("Ãp", "ãpple", false)]
    [InlineData("ap", "Ãpple", false)]
    [InlineData("app", "Ãpple", false)]//crdt fts only kicks in at 3 chars
    [InlineData("й", "й", false)] // D, C
    [InlineData("й", "й", false)] // C, D
    [InlineData("й", "й", true)] // C, C
    [InlineData("й", "й", true)] // D, D
    [InlineData("ймыл", "ймыл", false)] // D, C
    [InlineData("ймыл", "ймыл", false)] // C, D
    [InlineData("ймыл", "ймыл", true)] // C, C
    [InlineData("ймыл", "ймыл", true)] // D, D
    public async Task SuccessfulMatches(string searchTerm, string word, bool identical)
    {
        // identical is to make the test cases more readable when they only differ in their normalization
        (searchTerm == word).Should().Be(identical);
        // remove next line in https://github.com/sillsdev/languageforge-lexbox/issues/2065
        word = word.Normalize(NormalizationForm.FormD);
        await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = word } });
        var words = await Api.SearchEntries(searchTerm).Select(e => e.LexemeForm["en"]).ToArrayAsync();
        words.Should().NotBeEmpty();
        // Like LicLCM the MiniLcm API should normalize to NFD
        words.Should().Contain(word.Normalize(NormalizationForm.FormD));
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

    [Theory]
    [InlineData("word1", "word1", "word1")]
    [InlineData("app", "app,apple,banana", "app,apple")]
    [InlineData("apple", "app,apple,banana", "apple")]
    [InlineData("att", "battery,att,attack,zatt,rap:pratt", "att,attack,zatt,battery,rap")]
    [InlineData("a", "a,da,ma,aa,c:a,ti:a", "a,aa,da,ma,c,ti")]//test non fts search
    [InlineData("ap", "app,apple,banana", "app,apple")]//test non fts search
    [InlineData("at", "battery,att,attack,zatt,rap:pratt", "att,attack,zatt,battery,rap")] //test non fts search
    // matching headwords trump glosses, even if the headword match is penalized for other long fields
    [InlineData("aap", "maap-aap,maap,liaap,aap:to-penalize-the-bm25-rank-this-gloss-is-very-very-very-very-very-very-very-very-very-long", "aap,maap,liaap,maap-aap")]
    // matching headwords of the same length are ordered alphabetically
    [InlineData("aap", "aap,baap:penalize-so-it-should-come-after-caap,caap", "aap,baap,caap")]
    public async Task RankedOrder(string searchTerm, string wordsAndGlosses, string expectedOrder)
    {
        var ids = new HashSet<Guid>();
        var wordsAndGlossesSplit = wordsAndGlosses.Split(",").Select(w => w.Split(":"));
        foreach (var wordAndGloss in Faker.Faker.Random.Shuffle(wordsAndGlossesSplit))
        {
            wordAndGloss.Should().HaveCountLessThanOrEqualTo(2);
            var word = wordAndGloss[0];
            var id = Guid.NewGuid();
            ids.Add(id);
            var entry = new Entry() { Id = id, LexemeForm = { { "en", word } } };
            if (wordAndGloss.Length > 1)
                entry.Senses.Add(new Sense() { Gloss = { { "en", wordAndGloss[1] } } });
            await Api.CreateEntry(entry);
        }
        var result = await Api.SearchEntries(searchTerm, new(new(SortField.SearchRelevance)))
            .Where(e => ids.Contains(e.Id))//only include entries from this test
            .Select(e => e.LexemeForm["en"])
            .ToArrayAsync();
        string.Join(",", result).Should().Be(expectedOrder);
    }

    [Theory]
    [InlineData("word1", "word1", "word1")]
    [InlineData("app", "app,apple,banana", "app,apple")]
    [InlineData("apple", "app,apple,banana", "apple")]
    [InlineData("att", "battery,att,attack,zatt,rap:pratt", "att,attack,battery,rap,zatt")]
    [InlineData("a", "a,da,ma,aa,c:a,ti:a", "a,aa,c,da,ma,ti")]//test non fts search
    [InlineData("ap", "app,apple,banana", "app,apple")] //test non fts search
    [InlineData("at", "battery,att,attack,zatt,rap:pratt", "att,attack,battery,rap,zatt")] //test non fts search
    public async Task HeadwordOrder(string searchTerm, string wordsAndGlosses, string expectedOrder)
    {
        var ids = new HashSet<Guid>();
        var wordsAndGlossesSplit = wordsAndGlosses.Split(",").Select(w => w.Split(":"));
        foreach (var wordAndGloss in Faker.Faker.Random.Shuffle(wordsAndGlossesSplit))
        {
            wordAndGloss.Should().HaveCountLessThanOrEqualTo(2);
            var word = wordAndGloss[0];
            var id = Guid.NewGuid();
            ids.Add(id);
            var entry = new Entry() { Id = id, LexemeForm = { { "en", word } } };
            if (wordAndGloss.Length > 1)
                entry.Senses.Add(new Sense() { Gloss = { { "en", wordAndGloss[1] } } });
            await Api.CreateEntry(entry);
        }

        var result = (await Api.SearchEntries(searchTerm, new(new(SortField.Headword)))
                .ToArrayAsync())
                .Where(e => ids.Contains(e.Id)) //only include entries from this test
                .Select(e => e.LexemeForm["en"]);
        string.Join(",", result).Should().Be(expectedOrder);
    }

    [Theory]
    [InlineData("a;", "a;test")]  // Non-FTS search (2 chars total)
    [InlineData("abc;", "abc;test")] // FTS search (4 chars total)
    [InlineData("a:", "a:test")]  // Non-FTS with colon (2 chars total)
    [InlineData("abc:", "abc:test")] // FTS with colon (4 chars total)
    [InlineData("a\"", "a\"test")] // Non-FTS with quote (2 chars total)
    [InlineData("abc\"", "abc\"test")] // FTS with quote (4 chars total)
    [InlineData("a'", "a'test")]  // Non-FTS with apostrophe (U+0027, 2 chars total)
    [InlineData("abc'", "abc'test")] // FTS with apostrophe (U+0027, 4 chars total)
    public async Task PunctuationWorks(string searchTerm, string word)
    {
        await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = word } });
        var results = await Api.SearchEntries(searchTerm).Select(e => e.LexemeForm["en"]).ToArrayAsync();
        results.Should().Contain(word);
    }
}

// A seperate class to preserve the readability of the results in the main test class
public abstract class NullAndEmptyQueryEntryTestsBase : MiniLcmTestBase
{
    private readonly string Apple = "Apple";
    private readonly string Null = string.Empty; // nulls get normalized to empty strings
    private readonly string EmptyString = string.Empty;
    private readonly string NullString = "null";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Api.CreateEntry(new Entry() { LexemeForm = { { "en", Apple } } });
        // null / missing key
        await Api.CreateEntry(new Entry());
        // blank
        await Api.CreateEntry(new Entry() { LexemeForm = { ["en"] = EmptyString } });
        // null string
        await Api.CreateEntry(new Entry() { LexemeForm = { ["en"] = NullString } });
    }

    [Fact]
    public async Task CanFilterIsNullOrEmpty()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "LexemeForm[en]=" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Null, EmptyString);
    }

    [Fact]
    public async Task CanFilterIsNotNullOrEmpty()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "LexemeForm[en]!=" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(Apple, NullString);
    }

    [Fact]
    public async Task CanFilterEqualsNullString()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "LexemeForm[en]=null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(NullString);
    }

    [Fact]
    public async Task CanFilterNotEqualsNullString()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "LexemeForm[en]!=null" })).ToArrayAsync();
        // Sadly the != operator isn't consistent, but it's an edge case that probably isn't crucial:
        // crdt logic: key exists (and/or value is not null, I'm not sure exactly) && LexemeForm[en] != "null"
        // fwdata logic: LexemeForm[en] != "null"
        // i.e. the entry that doesn't have LexemeForm[en] at all, is only included in the fwdata results
        results.Select(e => e.LexemeForm["en"]).Should().BeSubsetOf([Apple, Null, EmptyString]);
        results.Count().Should().BeInRange(2, 3);
    }

    [Fact]
    public async Task CanFilterContainsNullString()
    {
        var results = await Api.GetEntries(new(Filter: new() { GridifyFilter = "LexemeForm[en]=*null" })).ToArrayAsync();
        results.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(NullString);
    }
}
