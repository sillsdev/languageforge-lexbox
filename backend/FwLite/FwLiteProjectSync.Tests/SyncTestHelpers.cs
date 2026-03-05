using FwDataMiniLcmBridge.Api;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

public static class SyncTestHelpers
{
    public static MorphType CreateMorphType(MorphTypeKind kind, string? prefix = null, string? postfix = null, int secondaryOrder = 0)
    {
        var guid = LcmHelpers.ToLcmMorphTypeId(kind) ?? Guid.NewGuid();
        var name = $"Test {kind}";
        var abbr = $"Tst {kind}";
        var desc = $"Test morph type {kind}";
        return new MorphType
        {
            Id = guid,
            Kind = kind,
            Name = new MultiString() { { "en", name } },
            Abbreviation = new MultiString() { { "en", abbr } },
            Description = new RichMultiString() { { "en", new RichString(desc) } },
            Prefix = prefix,
            Postfix = postfix,
            SecondaryOrder = secondaryOrder,
        };
    }

    public static MorphType UpdateMorphType(MorphType orig, string? newName = null, string? newAbbreviation = null)
    {
        var newby = orig.Copy();
        if (newName is not null) newby.Name["en"] = newName;
        if (newAbbreviation is not null) newby.Abbreviation["en"] = newAbbreviation;
        return newby;
    }
}
