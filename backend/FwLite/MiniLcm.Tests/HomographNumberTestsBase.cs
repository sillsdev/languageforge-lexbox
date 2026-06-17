namespace MiniLcm.Tests;

// Both FwData and CRDT handle basic homograph incrementing
// and promoting 0's to 1's when a second homograph is added
public abstract class HomographNumberTestsBase : MiniLcmTestBase
{
    [Fact]
    public async Task CreateEntry_HomographNumberZero_IsAutoAssignedAsHomographsAppear()
    {
        var a = await Api.CreateEntry(new() { LexemeForm = { { "en", "hgr" } }, HomographNumber = 0 });
        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(0, "a lone entry has no homographs");

        var b = await Api.CreateEntry(new() { LexemeForm = { { "en", "hgr" } }, HomographNumber = 0 });
        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(1);
        (await Api.GetEntry(b.Id))!.HomographNumber.Should().Be(2);

        var c = await Api.CreateEntry(new() { LexemeForm = { { "en", "hgr" } }, HomographNumber = 0 });
        (await Api.GetEntry(c.Id))!.HomographNumber.Should().Be(3);
    }

    [Fact]
    public async Task CreateEntry_DifferentSecondaryOrder_DoesNotShareHomographNumbers()
    {
        var rootEntry = await Api.CreateEntry(new() { LexemeForm = { { "en", "morphtest" } }, MorphType = MorphTypeKind.Root });
        rootEntry.HomographNumber.Should().Be(0, "lone Root entry should have HomographNumber 0");

        var boundRootEntry = await Api.CreateEntry(new() { LexemeForm = { { "en", "morphtest" } }, MorphType = MorphTypeKind.BoundRoot });
        boundRootEntry.HomographNumber.Should().Be(0, "BoundRoot with different SecondaryOrder should have HomographNumber 0");
    }

    [Fact]
    public async Task CreateEntry_AutoAssignsHomographNumber_WithCitationForm()
    {
        var entry1 = await Api.CreateEntry(new() { LexemeForm = { { "en", "cfLexeme1" } }, CitationForm = { { "en", "cfHomograph" } } });
        entry1.HomographNumber.Should().Be(0, "single entry should have HomographNumber 0");

        var entry2 = await Api.CreateEntry(new() { LexemeForm = { { "en", "cfLexeme2" } }, CitationForm = { { "en", "cfHomograph" } } });
        entry2.HomographNumber.Should().Be(2, "second entry with same CitationForm should get HomographNumber 2");

        entry1 = await Api.GetEntry(entry1.Id) ?? throw new NullReferenceException();
        entry1.HomographNumber.Should().Be(1, "first entry should be updated to HomographNumber 1");
    }
}
