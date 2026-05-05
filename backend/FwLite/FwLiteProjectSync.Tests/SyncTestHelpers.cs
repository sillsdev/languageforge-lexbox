using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

public static class SyncTestHelpers
{
    public static MorphType UpdateMorphType(MorphType orig, string? newName = null, string? newAbbreviation = null)
    {
        var newby = orig.Copy();
        if (newName is not null) newby.Name["en"] = newName;
        if (newAbbreviation is not null) newby.Abbreviation["en"] = newAbbreviation;
        return newby;
    }
}
