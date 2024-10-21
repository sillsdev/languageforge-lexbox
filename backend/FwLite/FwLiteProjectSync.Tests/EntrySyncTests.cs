using FwLiteProjectSync.SyncHelpers;
using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

public class EntrySyncTests : IClassFixture<SyncFixture>
{
    public EntrySyncTests(SyncFixture fixture)
    {
        fixture.DisableFwData();
        _fixture = fixture;
    }

    private readonly SyncFixture _fixture;

    [Fact]
    public async Task CanChangeComplexFormViaSync()
    {
        var component1 = await _fixture.CrdtApi.CreateEntry(new() { LexemeForm = { { "en", "component1" } } });
        var component2 = await _fixture.CrdtApi.CreateEntry(new() { LexemeForm = { { "en", "component2" } } });
        var complexFormId = Guid.NewGuid();
        var complexForm = await _fixture.CrdtApi.CreateEntry(new()
        {
            Id = complexFormId,
            LexemeForm = { { "en", "complex form" } },
            Components =
            [
                new ComplexFormComponent()
                {
                    ComponentEntryId = component1.Id,
                    ComponentHeadword = component1.Headword(),
                    ComplexFormEntryId = complexFormId,
                    ComplexFormHeadword = "complex form"
                }
            ]
        });
        Entry after = (Entry) complexForm.Copy();
        after.Components[0].ComponentEntryId = component2.Id;
        after.Components[0].ComponentHeadword = component2.Headword();

        await EntrySync.Sync(after, complexForm, _fixture.CrdtApi);

        var actual = await _fixture.CrdtApi.GetEntry(after.Id);
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(after);
    }
}
