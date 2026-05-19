using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class CreateEntryTests(ProjectLoaderFixture fixture) : CreateEntryTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("create-entry-test", "en", "en"));
    }

    [Fact]
    public async Task CreateEntry_SetsMorphType()
    {
        var entry = await Api.CreateEntry(new Entry() { LexemeForm = { ["en"] = "test" } });
        var fwDataApi = (FwDataMiniLcmApi)BaseApi;
        var lexEntry = fwDataApi.EntriesRepository.GetObject(entry.Id);
        lexEntry.Should().NotBeNull();
        lexEntry.LexemeFormOA.MorphTypeRA.Should().NotBeNull();
        lexEntry.LexemeFormOA.MorphTypeRA.Guid.Should().Be(MoMorphTypeTags.kguidMorphStem);
        lexEntry.LexemeFormOA.MorphTypeRA.Name.UiString.Should().Be("stem");

        lexEntry.PrimaryMorphType.Should().NotBeNull();
        lexEntry.PrimaryMorphType.Guid.Should().Be(MoMorphTypeTags.kguidMorphStem);
        lexEntry.PrimaryMorphType.Name.UiString.Should().Be("stem");
    }

    [Fact]
    public async Task CreateEntry_HomographNumberOutOfRange_IsClampedToMaxPlusOne()
    {
        const string form = "outOfRangeHnTest";
        var a = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        var b = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(1);
        (await Api.GetEntry(b.Id))!.HomographNumber.Should().Be(2);

        // Request HN=10 with only 2 existing homographs — should be corrected to current-max + 1 = 3.
        var c = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form }, HomographNumber = 10 });

        (await Api.GetEntry(c.Id))!.HomographNumber.Should().Be(3, "out-of-range HN is clamped to max + 1");
        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(1, "existing homographs keep their numbers");
        (await Api.GetEntry(b.Id))!.HomographNumber.Should().Be(2, "existing homographs keep their numbers");
    }

    [Fact]
    public async Task CreateEntry_DuplicateHomographNumber_IsDeduplicated()
    {
        const string form = "duplicateHnTest";
        var a = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        var b = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        var c = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form } });
        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(1);
        (await Api.GetEntry(b.Id))!.HomographNumber.Should().Be(2);
        (await Api.GetEntry(c.Id))!.HomographNumber.Should().Be(3);

        // Request HN=2, colliding with B. Renumber by (HN, DateCreated, Guid) shifts the newcomer
        // to 3 (next slot after the older HN=2) and bumps C to 4.
        var d = await Api.CreateEntry(new Entry { LexemeForm = { ["en"] = form }, HomographNumber = 2 });

        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(1);
        (await Api.GetEntry(b.Id))!.HomographNumber.Should().Be(2, "older HN=2 wins the slot");
        (await Api.GetEntry(d.Id))!.HomographNumber.Should().Be(3, "duplicate gets the next available slot");
        (await Api.GetEntry(c.Id))!.HomographNumber.Should().Be(4, "C is bumped to make room");
    }
}
