using MiniLcm.Exceptions;

namespace MiniLcm.Tests;

/// <summary>
/// Tests for GetEntriesWindow and GetEntryIndex APIs.
/// These APIs are critical for chunk-based virtual scrolling.
/// </summary>
public abstract class EntryWindowTestsBase : MiniLcmTestBase
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
        // Create entries in non-alphabetical order to test sorting
        await Api.CreateEntry(new Entry { Id = orangeId, LexemeForm = { { "en", Orange } } });
        await Api.CreateEntry(new Entry { Id = appleId, LexemeForm = { { "en", Apple } } });
        await Api.CreateEntry(new Entry { Id = kiwiId, LexemeForm = { { "en", Kiwi } } });
        await Api.CreateEntry(new Entry { Id = bananaId, LexemeForm = { { "en", Banana } } });
        await Api.CreateEntry(new Entry { Id = peachId, LexemeForm = { { "en", Peach } } });
    }

    [Fact]
    public async Task GetEntriesWindow_FirstWindow_ReturnsFirstChunk()
    {
        var result = await Api.GetEntriesWindow(0, 2);

        result.FirstIndex.Should().Be(0);
        result.Entries.Should().HaveCount(2);
        // Entries should be sorted alphabetically by headword
        result.Entries[0].LexemeForm["en"].Should().Be(Apple);
        result.Entries[1].LexemeForm["en"].Should().Be(Banana);
    }

    [Fact]
    public async Task GetEntriesWindow_SecondWindow_ReturnsSecondChunk()
    {
        var result = await Api.GetEntriesWindow(2, 2);

        result.FirstIndex.Should().Be(2);
        result.Entries.Should().HaveCount(2);
        result.Entries[0].LexemeForm["en"].Should().Be(Kiwi);
        result.Entries[1].LexemeForm["en"].Should().Be(Orange);
    }

    [Fact]
    public async Task GetEntriesWindow_PartialLastWindow_ReturnsRemainingEntries()
    {
        var result = await Api.GetEntriesWindow(4, 2);

        result.FirstIndex.Should().Be(4);
        result.Entries.Should().HaveCount(1);
        result.Entries[0].LexemeForm["en"].Should().Be(Peach);
    }

    [Fact]
    public async Task GetEntriesWindow_EmptyWindow_ReturnsEmptyList()
    {
        var result = await Api.GetEntriesWindow(10, 2);

        result.FirstIndex.Should().Be(10);
        result.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEntriesWindow_WindowSizeOfOne_WorksCorrectly()
    {
        var result = await Api.GetEntriesWindow(1, 1);

        result.FirstIndex.Should().Be(1);
        result.Entries.Should().HaveCount(1);
        result.Entries[0].LexemeForm["en"].Should().Be(Banana);
    }

    [Fact]
    public async Task GetEntriesWindow_LargeWindow_ReturnsBiggerChunk()
    {
        var result = await Api.GetEntriesWindow(0, 10);

        result.FirstIndex.Should().Be(0);
        result.Entries.Should().HaveCount(5);
        // Verify all entries are present in alphabetical order
        result.Entries.Select(e => e.LexemeForm["en"])
            .Should().ContainInOrder(Apple, Banana, Kiwi, Orange, Peach);
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
    public async Task GetEntriesWindow_WithOffset_SkipsEntries()
    {
        var result = await Api.GetEntriesWindow(1, 3);

        result.FirstIndex.Should().Be(1);
        result.Entries.Should().HaveCount(3);
        result.Entries.Select(e => e.LexemeForm["en"])
            .Should().ContainInOrder(Banana, Kiwi, Orange);
    }

    [Fact]
    public async Task GetEntriesWindow_ReturnedEntriesAreComplete()
    {
        var result = await Api.GetEntriesWindow(0, 1);

        result.Entries.Should().HaveCount(1);
        var entry = result.Entries[0];
        entry.Id.Should().NotBe(Guid.Empty);
        entry.LexemeForm.Should().NotBeNull();
        entry.Senses.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEntriesWindow_MultipleCallsCoverAllEntries()
    {
        // Simulate chunking through all entries
        var window1 = await Api.GetEntriesWindow(0, 2);
        var window2 = await Api.GetEntriesWindow(2, 2);
        var window3 = await Api.GetEntriesWindow(4, 2);

        var allHeadwords = new List<string>();
        allHeadwords.AddRange(window1.Entries.Select(e => e.LexemeForm["en"]));
        allHeadwords.AddRange(window2.Entries.Select(e => e.LexemeForm["en"]));
        allHeadwords.AddRange(window3.Entries.Select(e => e.LexemeForm["en"]));

        allHeadwords.Should().BeEquivalentTo(Apple, Banana, Kiwi, Orange, Peach);
    }

    [Fact]
    public async Task GetEntryIndex_CanPositionWindow()
    {
        // Get index for an entry
        var rowIndex = await Api.GetEntryIndex(kiwiId);

        // Use it to fetch a window around that entry
        var windowResult = await Api.GetEntriesWindow(rowIndex, 2);

        windowResult.FirstIndex.Should().Be(2);
        windowResult.Entries.Should().HaveCount(2);
        windowResult.Entries[0].Id.Should().Be(kiwiId);
        windowResult.Entries[1].LexemeForm["en"].Should().Be(Orange);
    }
}
