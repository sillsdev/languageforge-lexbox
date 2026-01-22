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

    protected async Task CreateStandardEntries()
    {
        await Api.CreateEntry(new Entry { Id = appleId, LexemeForm = { { "en", Apple } } });
        await Api.CreateEntry(new Entry { Id = bananaId, LexemeForm = { { "en", Banana } } });
        await Api.CreateEntry(new Entry { Id = kiwiId, LexemeForm = { { "en", Kiwi } } });
        await Api.CreateEntry(new Entry { Id = orangeId, LexemeForm = { { "en", Orange } } });
        await Api.CreateEntry(new Entry { Id = peachId, LexemeForm = { { "en", Peach } } });
    }

    [Fact]
    public async Task GetEntryIndex_FirstEntry_ReturnsZero()
    {
        await CreateStandardEntries();
        var result = await Api.GetEntryIndex(appleId);

        result.Should().Be(0);
    }

    [Fact]
    public async Task GetEntryIndex_MiddleEntry_ReturnsCorrectIndex()
    {
        await CreateStandardEntries();
        var result = await Api.GetEntryIndex(kiwiId);

        result.Should().Be(2);
    }

    [Fact]
    public async Task GetEntryIndex_LastEntry_ReturnsLastIndex()
    {
        await CreateStandardEntries();
        var result = await Api.GetEntryIndex(peachId);

        result.Should().Be(4);
    }

    [Fact]
    public async Task GetEntryIndex_NonExistentEntry_ReturnsNegativeOne()
    {
        await CreateStandardEntries();
        var nonExistentId = Guid.NewGuid();

        var result = await Api.GetEntryIndex(nonExistentId);

        result.Should().Be(-1);
    }

    [Fact]
    public async Task GetEntryIndex_WithCustomSort_ReturnsCorrectIndex()
    {
        await CreateStandardEntries();

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
            // Ensure deterministic sort (e.g., Entry 0000, 0001, ...)
            await Api.CreateEntry(new Entry { Id = id, LexemeForm = { { "en", $"Entry {i:D4}" } } });
        }

        (await Api.GetEntryIndex(entryIds[0])).Should().Be(0);
        (await Api.GetEntryIndex(entryIds[500])).Should().Be(500);
        (await Api.GetEntryIndex(entryIds[1000])).Should().Be(1000);
        (await Api.GetEntryIndex(entryIds[1004])).Should().Be(1004);
    }
}
