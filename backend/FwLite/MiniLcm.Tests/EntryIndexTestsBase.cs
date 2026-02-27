using MiniLcm.Filtering;

namespace MiniLcm.Tests;

/// <summary>
/// Tests for GetEntryIndex API.
/// This API is critical for finding the position of an entry in a sorted/filtered list for virtual scrolling.
/// </summary>
public abstract class EntryIndexTestsBase : MiniLcmTestBase
{
    private readonly Guid appleId = Guid.NewGuid();
    private readonly Guid bananaId = Guid.NewGuid();
    private readonly Guid kiwiId = Guid.NewGuid();
    private readonly Guid orangeId = Guid.NewGuid();
    private readonly Guid peachId = Guid.NewGuid();

    private const string Apple = "Apple";
    private const string Banana = "Banana";
    private const string Kiwi = "Kiwi";
    private const string Orange = "Orange";
    private const string Peach = "Peach";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Api.CreateEntry(new Entry { Id = appleId, LexemeForm = { { "en", Apple } } });
        await Api.CreateEntry(new Entry { Id = bananaId, LexemeForm = { { "en", Banana } } });
        await Api.CreateEntry(new Entry { Id = kiwiId, LexemeForm = { { "en", Kiwi } } });
        await Api.CreateEntry(new Entry { Id = orangeId, LexemeForm = { { "en", Orange } } });
        await Api.CreateEntry(new Entry { Id = peachId, LexemeForm = { { "en", Peach } } });
    }

    [Fact]
    public async Task GetEntryIndex_FirstEntry_ReturnsZero()
    {
        var result = await Api.GetEntryIndex(appleId);

        result.Should().Be(0);
    }

    [Fact]
    public async Task GetEntryIndex_MiddleEntry_ReturnsCorrectIndex()
    {
        var result = await Api.GetEntryIndex(kiwiId);

        result.Should().Be(2);
    }

    [Fact]
    public async Task GetEntryIndex_LastEntry_ReturnsLastIndex()
    {
        var result = await Api.GetEntryIndex(peachId);

        result.Should().Be(4);
    }

    [Fact]
    public async Task GetEntryIndex_NonExistentEntry_ReturnsNegativeOne()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await Api.GetEntryIndex(nonExistentId);

        result.Should().Be(-1);
    }

    [Fact]
    public async Task GetEntryIndex_WithCustomSort_ReturnsCorrectIndex()
    {
        // Sort descending
        var options = new IndexQueryOptions(new SortOptions(SortField.Headword, "en", false));

        // Order: Peach, Orange, Kiwi, Banana, Apple
        (await Api.GetEntryIndex(peachId, null, options)).Should().Be(0);
        (await Api.GetEntryIndex(orangeId, null, options)).Should().Be(1);
        (await Api.GetEntryIndex(kiwiId, null, options)).Should().Be(2);
        (await Api.GetEntryIndex(bananaId, null, options)).Should().Be(3);
        (await Api.GetEntryIndex(appleId, null, options)).Should().Be(4);
    }

    [Fact]
    public async Task GetEntryIndex_LargeList_ReturnsCorrectIndex()
    {
        var entryIds = new List<Guid>();
        for (var i = 0; i < 1005; i++)
        {
            var id = Guid.NewGuid();
            entryIds.Add(id);
            // Ensure deterministic sort (e.g., 0000 Entry, 0001 Entry, ...)
            // We use a prefix to ensure these come before the standard entries (Apple, Banana, etc)
            await Api.CreateEntry(new Entry { Id = id, LexemeForm = { { "en", $"{i:D4} Entry" } } });
        }

        (await Api.GetEntryIndex(entryIds[0])).Should().Be(0);
        (await Api.GetEntryIndex(entryIds[500])).Should().Be(500);
        (await Api.GetEntryIndex(entryIds[1000])).Should().Be(1000);
        (await Api.GetEntryIndex(entryIds[1004])).Should().Be(1004);
    }

    [Fact]
    public async Task GetEntryIndex_WithShortQuery_ReturnsCorrectIndex()
    {
        // Query "Ki" is < 3 characters, should use simple search filter
        var result = await Api.GetEntryIndex(kiwiId, "Ki");

        // "Kiwi" should be the only match, index 0
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetEntryIndex_WithLongQuery_ReturnsCorrectIndex()
    {
        // Query "Orange" is >= 3 characters, should use FTS
        var result = await Api.GetEntryIndex(orangeId, "Orange");

        // "Orange" should be found, index 0
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetEntryIndex_WithGridifyFilter_ReturnsCorrectIndex()
    {
        // Use Gridify filter to find "banana"
        var options = new IndexQueryOptions(Filter: new EntryFilter { GridifyFilter = "LexemeForm[en]=Banana" });

        var result = await Api.GetEntryIndex(bananaId, null, options);

        result.Should().Be(0);
    }

    [Fact]
    public async Task GetEntryIndex_WithFilterAndSort_ReturnsCorrectIndex()
    {
        var options = new IndexQueryOptions(new SortOptions(SortField.SearchRelevance, "en", true));

        (await Api.GetEntryIndex(appleId, "a", options)).Should().Be(0);
        (await Api.GetEntryIndex(peachId, "a", options)).Should().Be(1);
        (await Api.GetEntryIndex(bananaId, "a", options)).Should().Be(2);
        (await Api.GetEntryIndex(orangeId, "a", options)).Should().Be(3);

        // "Kiwi" shouldn't be in the results at all
        (await Api.GetEntryIndex(kiwiId, "a", options)).Should().Be(-1);
    }

    [Fact]
    public async Task GetEntryIndex_WithFTSFilterAndSort_ReturnsCorrectIndex()
    {
        // Add some more entries to make search more interesting
        var applePieId = Guid.NewGuid();
        var pineappleId = Guid.NewGuid();
        await Api.CreateEntry(new Entry { Id = applePieId, LexemeForm = { { "en", "Apple Pie" } } });
        await Api.CreateEntry(new Entry { Id = pineappleId, LexemeForm = { { "en", "Pineapple" } } });

        // Query "Apple" (>= 3 chars, uses SearchService/FTS)
        // Matches: Apple (len 5), Apple Pie (len 9), Pineapple (len 9)
        // Sorting:
        // 1. Headword contains "Apple": All match.
        // 2. Length: Apple (5), Apple Pie (9), Pineapple (9)
        // 3. Alphabetical (for 9-len): Apple Pie, Pineapple
        var options = new IndexQueryOptions(new SortOptions(SortField.SearchRelevance, "en", true));

        (await Api.GetEntryIndex(appleId, "Apple", options)).Should().Be(0);
        (await Api.GetEntryIndex(applePieId, "Apple", options)).Should().Be(1);
        (await Api.GetEntryIndex(pineappleId, "Apple", options)).Should().Be(2);

        // "Banana" shouldn't be in the results
        (await Api.GetEntryIndex(bananaId, "Apple", options)).Should().Be(-1);
    }
}
