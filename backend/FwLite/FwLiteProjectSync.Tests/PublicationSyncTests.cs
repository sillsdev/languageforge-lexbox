using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteProjectSync.Tests;

public class PublicationSyncTests(SyncFixture fixture) : IClassFixture<SyncFixture>
{
    [Fact]
    public async Task DryRunSync_AddingANewMainPublication_DoesNotPromoteOrThrow()
    {
        // A main that's new to the collection is created with IsMain set, so it must not also be promoted via
        // UpdatePublication — during a dry run that publication doesn't exist yet, so the update would throw NotFound.
        var dryRunApi = new DryRunMiniLcmApi(fixture.CrdtApi);
        var newMain = new Publication { Id = Guid.NewGuid(), Name = { { "en", "Main" } }, IsMain = true };

        var act = () => PublicationSync.Sync([], [newMain], dryRunApi);

        await act.Should().NotThrowAsync();
        dryRunApi.DryRunRecords.Should().ContainSingle(r => r.Method == nameof(DryRunMiniLcmApi.CreatePublication));
        dryRunApi.DryRunRecords.Should().NotContain(r => r.Method == nameof(DryRunMiniLcmApi.UpdatePublication));
    }
}
