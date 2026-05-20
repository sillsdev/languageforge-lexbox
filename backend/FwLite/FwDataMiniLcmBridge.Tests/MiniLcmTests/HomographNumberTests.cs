using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

// FwData-specific behaviors: an out-of-range or duplicate request gets renumbered into a
// valid sequence, and an update to HN=0 backfills the entry into the gap it would leave.
// The shared HN=0 auto-assignment scenario (lone stays at 0; subsequent matches climb)
// lives in HomographNumberTestsBase.
[Collection(ProjectLoaderFixture.Name)]
public class HomographNumberTests(ProjectLoaderFixture fixture) : HomographNumberTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("homograph-number-test", "en", "en"));
    }

    [Fact]
    public async Task CreateEntry_HomographNumberOutOfRange_IsClampedToMaxPlusOne()
    {
        const string form = "createOutOfRangeHnTest";
        var a = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        var b = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(1);
        (await Api.GetEntry(b.Id))!.HomographNumber.Should().Be(2);

        // Request HN=10 with only 2 existing homographs — should be corrected to current-max + 1 = 3.
        var c = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form }, HomographNumber = 10 });

        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(1);
        (await Api.GetEntry(b.Id))!.HomographNumber.Should().Be(2);
        (await Api.GetEntry(c.Id))!.HomographNumber.Should().Be(3, "out-of-range HN is clamped to max + 1");
    }

    [Fact]
    public async Task CreateEntry_DuplicateHomographNumber_IsDeduplicated()
    {
        const string form = "createDuplicateHnTest";
        var a = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        var b = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        var c = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(1);
        (await Api.GetEntry(b.Id))!.HomographNumber.Should().Be(2);
        (await Api.GetEntry(c.Id))!.HomographNumber.Should().Be(3);

        // Request HN=2, colliding with B.
        var d = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form }, HomographNumber = 2 });

        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(1);
        (await Api.GetEntry(b.Id))!.HomographNumber.Should().Be(2, "older HN=2 keeps its slot");
        (await Api.GetEntry(d.Id))!.HomographNumber.Should().Be(3, "duplicate slots in after the senior holder");
        (await Api.GetEntry(c.Id))!.HomographNumber.Should().Be(4, "C is shoved up to keep the sequence valid");
    }

    [Fact]
    public async Task UpdateEntry_HomographNumberOutOfRange_IsClampedBackIntoRange()
    {
        const string form = "updateOutOfRangeHnTest";
        var a = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        var b = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        var c = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        // baseline: A=1, B=2, C=3

        // Update the newest (C) to an out-of-range HN; renumber sorts it back to the end.
        await Api.UpdateEntry(c, c with { HomographNumber = 10 });

        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(1);
        (await Api.GetEntry(b.Id))!.HomographNumber.Should().Be(2);
        (await Api.GetEntry(c.Id))!.HomographNumber.Should().Be(3, "out-of-range update is clamped back into range");
    }

    [Fact]
    public async Task UpdateEntry_HomographNumberZero_BackfillsIntoTheGap()
    {
        const string form = "updateZeroHnTest";
        var a = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        var b = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        var c = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        // baseline: A=1, B=2, C=3

        // Setting C's HN to 0 leaves a gap at 3; renumber fills C back in.
        await Api.UpdateEntry(c, c with { HomographNumber = 0 });

        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(1);
        (await Api.GetEntry(b.Id))!.HomographNumber.Should().Be(2);
        (await Api.GetEntry(c.Id))!.HomographNumber.Should().Be(3, "HN=0 is backfilled into the gap it left behind");
    }

    [Fact]
    public async Task UpdateEntry_DuplicateHomographNumber_IsDeduplicated()
    {
        const string form = "updateDuplicateHnTest";
        var a = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        var b = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        var c = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        // baseline: A=1, B=2, C=3

        // Update C (newest) to duplicate A's HN=1.
        await Api.UpdateEntry(c, c with { HomographNumber = 1 });

        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(1, "older HN=1 keeps its slot");
        (await Api.GetEntry(c.Id))!.HomographNumber.Should().Be(2, "duplicate slots in after the senior holder");
        (await Api.GetEntry(b.Id))!.HomographNumber.Should().Be(3, "B is shoved up to keep the sequence valid");
    }
}
