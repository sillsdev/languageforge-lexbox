namespace MiniLcm.Tests;

public abstract class HomographNumberTestsBase : MiniLcmTestBase
{
    // Both implementations should agree on how HN=0 (or unspecified) is handled when entries
    // share a headword: a lone entry keeps 0, a second matching entry produces a 1/2 pair, and
    // subsequent matches pick up where the cluster's max leaves off. Non-zero requests, and
    // updates that introduce duplicates or out-of-range values, are impl-specific and covered
    // in the concrete classes.
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
}
