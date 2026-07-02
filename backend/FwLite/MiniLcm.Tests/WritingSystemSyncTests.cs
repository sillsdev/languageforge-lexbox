using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace MiniLcm.Tests;

public class WritingSystemSyncTests
{
    private static WritingSystem BaseWs() => new()
    {
        Id = Guid.NewGuid(),
        WsId = "en",
        Name = "English",
        Abbreviation = "En",
        Font = "Arial",
        Type = WritingSystemType.Vernacular,
    };

    [Fact]
    public void DiffToUpdate_IncludesIcuCollationRules()
    {
        var before = BaseWs();
        var after = before with { IcuCollationRules = "&a < b" };

        var update = WritingSystemSync.WritingSystemDiffToUpdate(before, after);

        update.Should().NotBeNull();
        var op = update!.Patch.Operations.Single(o => o.Path == $"/{nameof(WritingSystem.IcuCollationRules)}");
        op.Value?.ToString().Should().Be("&a < b");
    }

    [Fact]
    public void DiffToUpdate_IncludesSystemCollationLocale()
    {
        var before = BaseWs();
        var after = before with { SystemCollationLocale = "de" };

        var update = WritingSystemSync.WritingSystemDiffToUpdate(before, after);

        update.Should().NotBeNull();
        var op = update!.Patch.Operations.Single(o => o.Path == $"/{nameof(WritingSystem.SystemCollationLocale)}");
        op.Value?.ToString().Should().Be("de");
    }

    [Fact]
    public void DiffToUpdate_NoOpsWhenCollationUnchanged()
    {
        var before = BaseWs() with { IcuCollationRules = "&a < b" };
        var after = before with { };

        var update = WritingSystemSync.WritingSystemDiffToUpdate(before, after);

        update.Should().BeNull();
    }
}
