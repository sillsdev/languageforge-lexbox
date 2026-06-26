using LcmCrdt.FullTextSearch;
using Microsoft.EntityFrameworkCore;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;

namespace LcmCrdt.Tests.FullTextSearch;

public class EntrySearchServiceTests : IAsyncLifetime
{
    private readonly MiniLcmApiFixture fixture = new();

    private static readonly AutoFaker Faker = new(AutoFakerDefault.Config);

    private readonly Entry _entry = Faker.Generate<Entry>();
    private EntrySearchService _service = null!;
    private LcmCrdtDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await fixture.InitializeAsync();
        await fixture.Api.CreateWritingSystem(new WritingSystem()
        {
            Id = Guid.NewGuid(),
            WsId = "fr",
            Name = "French",
            Abbreviation = "fr",
            Font = "Arial",
            Exemplars = ["a", "b"],
            Type = WritingSystemType.Vernacular
        });
        await fixture.Api.CreateWritingSystem(new WritingSystem()
        {
            Id = Guid.NewGuid(),
            WsId = "es",
            Name = "Spanish",
            Abbreviation = "es",
            Font = "Arial",
            Exemplars = ["a", "b"],
            Type = WritingSystemType.Analysis
        });
        _context = await fixture.GetService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        _service = fixture.GetService<EntrySearchServiceFactory>().CreateSearchService(_context);
    }

    [Fact]
    public async Task CanUpdateAnEntrySearchRecord()
    {
        await _service.UpdateEntrySearchTable(_entry);
        var result = await _service.EntrySearchRecords.AsAsyncEnumerable().ToArrayAsync();
        result.Should().Contain(e => e.Id == _entry.Id);
    }

    [Fact]
    public async Task CanUpdateAnExistingEntrySearchRecord()
    {
        var entry = new Entry() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "initial" } };
        await _service.UpdateEntrySearchTable(entry);
        var updated = entry.Copy();
        updated.LexemeForm["en"] = "updated";
        await _service.UpdateEntrySearchTable(updated);
        var result = await _service.EntrySearchRecords
            .AsAsyncEnumerable()
            .SingleOrDefaultAsync(e => e.Id == entry.Id);
        result.Should().NotBeNull();
        result.LexemeForm.Should().Be("updated");
    }

    [Fact]
    public async Task UpdateEntrySearchTableEnumerable_DoesNotCreateDuplicates()
    {
        var entry = new Entry() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "initial" } };
        await _service.UpdateEntrySearchTable(entry);
        var updated = entry.Copy();
        updated.LexemeForm["en"] = "updated";
        await _service.UpdateEntrySearchTable([updated]);
        var result = await _service.EntrySearchRecords.SingleOrDefaultAsync(e => e.Id == entry.Id);
        result.Should().NotBeNull();
        result.LexemeForm.Should().Be("updated");
    }

    [Fact]
    [Trait("Category", "Verified")]
    public async Task SearchTableIsUpdatedAutomaticallyOnInsert()
    {
        var id = Guid.NewGuid();
        _context.Set<Entry>().Add(new Entry()
        {
            Id = id,
            LexemeForm = { ["en"] = "lexemeform1", ["fr"] = "fr_lexemeform1" },
            CitationForm = { ["en"] = "citation1", ["fr"] = "fr_citation1" },
            Senses =
            [
                new Sense()
                {
                    Gloss = { ["en"] = "gloss1", ["es"] = "es_gloss1" },
                    Definition =
                    {
                        ["en"] = new RichString("definition1", "en"),
                        ["es"] = new RichString("es_definition1", "es")
                    }
                },
                new Sense()
                {
                    Gloss = { ["es"] = "es_gloss2" },
                    Definition =
                    {
                        ["en"] = new RichString("definition2", "en"),
                        ["es"] = new RichString("es_definition2", "es")
                    }
                }
            ]
        });
        await _context.SaveChangesAsync();
        var entry = await _service.EntrySearchRecords.SingleOrDefaultAsync(e => e.Id == id);
        await Verify(entry);
    }

    [Fact]
    [Trait("Category", "Verified")]
    public async Task SearchTableIsUpdatedAutomaticallyOnUpdate()
    {
        var id = Guid.NewGuid();
        _context.Set<Entry>().Add(new Entry()
        {
            Id = id,
            LexemeForm = { ["en"] = "lexemeform1", ["fr"] = "fr_lexemeform1" },
            CitationForm = { ["en"] = "citation1", ["fr"] = "fr_citation1" }
        });
        await _context.SaveChangesAsync();
        var entry = await _context.FindAsync<Entry>(id);
        entry.Should().NotBeNull();
        entry.LexemeForm["en"] = "lexemeform2";
        _context.Update(entry);
        await _context.SaveChangesAsync();

        //intentionally using a list to ensure there's only 1 as expected
        var entries = _service.EntrySearchRecords.Where(e => e.Id == id);
        await Verify(entries);
    }

    [Fact]
    public async Task RegeneratesSearchRecords_WhenMorphTypePrefixChanges()
    {
        // Suffix morph type has Prefix="-" by default, so a lexeme-only entry's headword is "-cat".
        var id = Guid.NewGuid();
        await fixture.Api.CreateEntry(new Entry()
        {
            Id = id,
            LexemeForm = { ["en"] = "cat" },
            MorphType = MorphTypeKind.Suffix
        });
        (await Headword(id)).Should().Be("-cat");

        var suffix = await fixture.Api.GetMorphType(MorphTypeKind.Suffix);
        suffix.Should().NotBeNull();
        var updated = suffix!.Copy();
        updated.Prefix = "~";
        await fixture.Api.UpdateMorphType(suffix, updated);

        // Changing the prefix token must regenerate the search record's headword.
        (await Headword(id)).Should().Be("~cat");
    }

    [Fact]
    public async Task RegeneratesSearchRecords_WhenMorphTypePostfixChanges()
    {
        // Prefix morph type has Postfix="-" by default, so a lexeme-only entry's headword is "un-".
        var id = Guid.NewGuid();
        await fixture.Api.CreateEntry(new Entry()
        {
            Id = id,
            LexemeForm = { ["en"] = "un" },
            MorphType = MorphTypeKind.Prefix
        });
        (await Headword(id)).Should().Be("un-");

        var prefix = await fixture.Api.GetMorphType(MorphTypeKind.Prefix);
        prefix.Should().NotBeNull();
        var updated = prefix!.Copy();
        updated.Postfix = "~";
        await fixture.Api.UpdateMorphType(prefix, updated);

        // Changing the postfix token must regenerate the search record's headword.
        (await Headword(id)).Should().Be("un~");
    }

    [Fact]
    public async Task RegeneratesSearchRecords_WhenMorphTypePrefixAndPostfixChange()
    {
        // Infix morph type has Prefix="-" and Postfix="-" by default, so the headword is "-in-".
        var id = Guid.NewGuid();
        await fixture.Api.CreateEntry(new Entry()
        {
            Id = id,
            LexemeForm = { ["en"] = "in" },
            MorphType = MorphTypeKind.Infix
        });
        (await Headword(id)).Should().Be("-in-");

        var infix = await fixture.Api.GetMorphType(MorphTypeKind.Infix);
        infix.Should().NotBeNull();
        var updated = infix!.Copy();
        updated.Prefix = "~";
        updated.Postfix = "~";
        await fixture.Api.UpdateMorphType(infix, updated);

        // Changing both tokens in a single update must regenerate the search record's headword.
        (await Headword(id)).Should().Be("~in~");
    }

    [Fact]
    public async Task SearchTableIsUpdatedAutomaticallyOnMorphTypeChange()
    {
        var id = Guid.NewGuid();
        await fixture.Api.CreateEntry(new Entry()
        {
            Id = id,
            LexemeForm = { ["en"] = "in" },
            MorphType = MorphTypeKind.Infix
        });
        (await Headword(id)).Should().Be("-in-");

        var entry = await _context.FindAsync<Entry>(id);
        entry.Should().NotBeNull();
        var updated = entry.Copy();
        updated.MorphType = MorphTypeKind.Simulfix; // Prefix and Postfix are "="
        await fixture.Api.UpdateEntry(entry, updated);

        // Changing morph type of an entry must regenerate its search record's headword.
        (await Headword(id)).Should().Be("=in=");
    }

    [Fact]
    public async Task SearchTableIsUpdatedAutomaticallyWhenManyChangesHappenOneAfterAnother()
    {
        var id = Guid.NewGuid();
        await fixture.Api.CreateEntry(new Entry()
        {
            Id = id,
            LexemeForm = { ["en"] = "in" },
            MorphType = MorphTypeKind.Infix
        });
        (await Headword(id)).Should().Be("-in-");

        var simulfix = await fixture.Api.GetMorphType(MorphTypeKind.Simulfix);
        simulfix.Should().NotBeNull();
        var updatedMorphType = simulfix!.Copy();
        updatedMorphType.Prefix = "~";
        updatedMorphType.Postfix = "~";
        await fixture.Api.UpdateMorphType(simulfix, updatedMorphType);

        var entry = await _context.FindAsync<Entry>(id);
        entry.Should().NotBeNull();
        var updated = entry.Copy();
        updated.LexemeForm = new() { ["en"] = "out" };
        updated.MorphType = MorphTypeKind.Simulfix;
        await fixture.Api.UpdateEntry(entry, updated);

        // Changing morph type of an entry must regenerate its search record's headword.
        (await Headword(id)).Should().Be("~out~");
    }

    [Fact]
    public async Task SearchTableIsUpdatedAutomaticallyWhenManyChangesHappenAtOnce_MorphTypeFirstThenEntry()
    {
        var id = Guid.NewGuid();
        await fixture.Api.CreateEntry(new Entry()
        {
            Id = id,
            LexemeForm = { ["en"] = "in" },
            MorphType = MorphTypeKind.Infix
        });
        (await Headword(id)).Should().Be("-in-");

        // Here we make changes directly in the DbContext, then call SaveChanges once at the end
        var simulfix = await _context.Set<MorphType>().FirstOrDefaultAsync(mt => mt.Kind == MorphTypeKind.Simulfix);
        simulfix.Should().NotBeNull();
        simulfix.Prefix = "~";
        simulfix.Postfix = "~";
        _context.Update(simulfix);

        var entry = await _context.FindAsync<Entry>(id);
        entry.Should().NotBeNull();
        entry.LexemeForm = new() { ["en"] = "out" };
        entry.MorphType = MorphTypeKind.Simulfix;
        _context.Update(entry);
        await _context.SaveChangesAsync();

        // Changing morph type of an entry must regenerate its search record's headword.
        (await Headword(id)).Should().Be("~out~");
    }

    [Fact]
    public async Task SearchTableIsUpdatedAutomaticallyWhenManyChangesHappenAtOnce_EntryFirstThenMorphType()
    {
        var id = Guid.NewGuid();
        await fixture.Api.CreateEntry(new Entry()
        {
            Id = id,
            LexemeForm = { ["en"] = "in" },
            MorphType = MorphTypeKind.Infix
        });
        (await Headword(id)).Should().Be("-in-");

        // Here we make changes directly in the DbContext, then call SaveChanges once at the end
        var entry = await _context.FindAsync<Entry>(id);
        entry.Should().NotBeNull();
        entry.LexemeForm = new() { ["en"] = "out" };
        entry.MorphType = MorphTypeKind.Simulfix;
        _context.Update(entry);

        var simulfix = await _context.Set<MorphType>().FirstOrDefaultAsync(mt => mt.Kind == MorphTypeKind.Simulfix);
        simulfix.Should().NotBeNull();
        simulfix.Prefix = "~";
        simulfix.Postfix = "~";
        _context.Update(simulfix);
        await _context.SaveChangesAsync();

        // Changing morph type of an entry must regenerate its search record's headword.
        (await Headword(id)).Should().Be("~out~");
    }

    private async Task<string?> Headword(Guid entryId)
    {
        // .AsNoTracking() needed here because RegenerateEntrySearchTable() has just cleared
        // and recreated the table using Linq2DB, but EF Core doesn't know that yet and so it
        // will serve us the cached copy of the table. So .AsNoTracking() ensures that EF Core
        // will hit the database and retrieve a fresh copy of the headword.
        var record = await _service.EntrySearchRecords.AsNoTracking().SingleOrDefaultAsync(e => e.Id == entryId);
        record.Should().NotBeNull();
        return record!.Headword;
    }

    [Fact]
    public async Task CanRegenerateTheSearchTable()
    {
        var id = Guid.NewGuid();
        _context.Set<Entry>().Add(new Entry()
        {
            Id = id,
            LexemeForm = { ["en"] = "word1" },
        });
        await _context.SaveChangesAsync();
        await _service.RemoveSearchRecord(id);
        _service.EntrySearchRecords.Should().NotContain(e => e.Id == id);
        await _service.RegenerateIfMissing();
        _service.EntrySearchRecords.Should().Contain(e => e.Id == id);
    }

    [Theory]
    [InlineData("word1", "word1", true)]
    [InlineData("ทำอาหาร", "ทำอาหาร", true)]
    [InlineData("ทำอา", "ทำอาหาร", true)]
    public async Task MatchWorksAsExpected(string searchTerm, string word, bool matches)
    {
        var id = Guid.NewGuid();
        await _service.UpdateEntrySearchTable(new Entry() { Id = id, LexemeForm = { { "en", word } } });


        var result = await _service.Search(searchTerm).ToArrayAsync();
        if (matches)
        {
            result.Should().Contain(e => e.Id == id);
        }
        else
        {
            result.Should().NotContain(e => e.Id == id);
        }
    }

    [Theory]
    [InlineData("lexemeform_en", true)]
    [InlineData("lexemeform_fr", true)]
    [InlineData("citation_fr", true)]
    [InlineData("CitationForm: citation", true)]
    [InlineData("lexemeform_de", false)]
    [InlineData("gloss", true)]
    [InlineData("definition", true)]
    [InlineData("Gloss: definition", false)]
    [InlineData("Definition: def", true)]
    [InlineData("LexemeForm: cit OR CitationForm: cit", true)]
    public async Task MatchColumnWorksAsExpected(string searchTerm, bool matches)
    {
        var id = Guid.NewGuid();
        await _service.UpdateEntrySearchTable(new Entry()
        {
            Id = id,
            LexemeForm = { { "en", "lexemeform_en" }, { "fr", "lexemeform_fr" } },
            CitationForm = { { "fr", "citation_fr" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "gloss" } }, Definition = { { "en", new RichString("definition", "en") } } }
            ]
        });


        var result = await _service.Search(searchTerm).ToArrayAsync();
        if (matches)
        {
            result.Should().Contain(e => e.Id == id);
        }
        else
        {
            result.Should().NotContain(e => e.Id == id);
        }
    }

    [Theory]
    [InlineData("word1", "word1", "word1")]
    [InlineData("app", "app,apple,banana", "app,apple")]
    [InlineData("apple", "app,apple,banana", "apple")]
    [InlineData("att", "att,attack,battery", "att,attack,battery")]
    [InlineData("att*", "att,attack,battery", "att,attack,battery")]
    [InlineData("att NOT attack", "att,attack,battery", "att,battery")]
    [InlineData("battery OR attack", "att,attack,battery", "attack,battery")]
    [InlineData("word1 word3", "word1 word2 word3,word4", "word1 word2 word3")]
    public async Task RanksResultsAsExpected(string searchTerm, string words, string expectedOrder)
    {
        var ids = new HashSet<Guid>();


        foreach (var word in Faker.Faker.Random.Shuffle(words.Split(",")))
        {
            var id = Guid.NewGuid();
            ids.Add(id);
            await _service.UpdateEntrySearchTable(new Entry() { Id = id, LexemeForm = { { "en", word } } });
        }

        var result = await _service.Search(searchTerm)
            .Where(e => ids.Contains(e.Id))//only include the entries we added to the search table, there may be others from other tests.
            .ToArrayAsync();
        string.Join(",", result.Select(e => e.LexemeForm)).Should().Be(expectedOrder);
    }

    [Fact]
    public async Task RanksResultsByColumn()
    {
        var word = Guid.NewGuid().ToString("N");
        var headword = Guid.NewGuid();
        var citationForm = Guid.NewGuid();
        var lexemeForm = Guid.NewGuid();
        var gloss = Guid.NewGuid();
        var definition = Guid.NewGuid();
        //only en is used for the headword
        await fixture.Api.CreateEntry(new Entry() { Id = headword, LexemeForm = { { "en", word } } });
        //equivalent to headword
        await fixture.Api.CreateEntry(new Entry() { Id = citationForm, CitationForm = { { "en", word } } });
        //using citation form ensures the matching lexeme-form isn't in the headword
        await fixture.Api.CreateEntry(new Entry() { Id = lexemeForm, LexemeForm = { { "en", word } }, CitationForm = { { "en", "❌" } } });
        await fixture.Api.CreateEntry(new Entry() { Id = definition, Senses = { new Sense() { Definition = { { "en", new RichString(word, "en") } } } } });
        await fixture.Api.CreateEntry(new Entry() { Id = gloss, Senses = { new Sense() { Gloss = { { "en", word } } } } });

        // Raw FTS rank search and sort
        var rawFtsResult = await _service.Search(word).ToArrayAsync();
        rawFtsResult.Select(e => Named(e.Id)).Should()
            .Equal(["headword", "headword", "lexemeform", "gloss", "definition"]);

        // The "real" search and sort method used by FW-Lite
        // Note: definition matches are not included by FilterAndRank
        var rankedResult = await fixture.Api.SearchEntries(word,
            new(new(SortField.SearchRelevance))).ToArrayAsync();
        rankedResult.Select(e => Named(e.Id)).Should()
            .Equal(["headword", "headword", "lexemeform", "gloss"]);

        string Named(Guid id)
        {
            return id switch
            {
                _ when id == headword => "headword",
                _ when id == citationForm => "headword", // always used as headword
                _ when id == lexemeForm => "lexemeform",
                _ when id == gloss => "gloss",
                _ when id == definition => "definition",
                _ => "unknown"
            };
        }

    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await fixture.DisposeAsync();
    }
}
