using System.Collections.Frozen;

namespace MiniLcm.Models;

/// <summary>
/// Canonical morph-type definitions matching FieldWorks/LibLCM MoMorphTypeTags.
/// GUIDs match SIL.LCModel constants (kguidMorph*). Data verified against Sena 3 FwData project.
/// </summary>
public static class CanonicalMorphTypes
{
    public static readonly FrozenDictionary<MorphTypeKind, MorphType> All = CreateAll().ToFrozenDictionary(m => m.Kind);

    private static MorphType[] CreateAll() =>
    [
        new()
        {
            Id = new Guid("d7f713e4-e8cf-11d3-9764-00c04f186933"),
            Kind = MorphTypeKind.BoundRoot,
            Name = new MultiString { { "en", "bound root" } },
            Abbreviation = new MultiString { { "en", "bd root" } },
            Prefix = "*",
            SecondaryOrder = 10,
        },
        new()
        {
            Id = new Guid("d7f713e7-e8cf-11d3-9764-00c04f186933"),
            Kind = MorphTypeKind.BoundStem,
            Name = new MultiString { { "en", "bound stem" } },
            Abbreviation = new MultiString { { "en", "bd stem" } },
            Prefix = "*",
            SecondaryOrder = 10,
        },
        new()
        {
            Id = new Guid("d7f713df-e8cf-11d3-9764-00c04f186933"),
            Kind = MorphTypeKind.Circumfix,
            Name = new MultiString { { "en", "circumfix" } },
            Abbreviation = new MultiString { { "en", "cfx" } },
        },
        new()
        {
            Id = new Guid("c2d140e5-7ca9-41f4-a69a-22fc7049dd2c"),
            Kind = MorphTypeKind.Clitic,
            Name = new MultiString { { "en", "clitic" } },
            Abbreviation = new MultiString { { "en", "clit" } },
        },
        new()
        {
            Id = new Guid("d7f713e1-e8cf-11d3-9764-00c04f186933"),
            Kind = MorphTypeKind.Enclitic,
            Name = new MultiString { { "en", "enclitic" } },
            Abbreviation = new MultiString { { "en", "enclit" } },
            Prefix = "=",
            SecondaryOrder = 80,
        },
        new()
        {
            Id = new Guid("d7f713da-e8cf-11d3-9764-00c04f186933"),
            Kind = MorphTypeKind.Infix,
            Name = new MultiString { { "en", "infix" } },
            Abbreviation = new MultiString { { "en", "ifx" } },
            Prefix = "-",
            Postfix = "-",
            SecondaryOrder = 40,
        },
        new()
        {
            Id = new Guid("56db04bf-3d58-44cc-b292-4c8aa68538f4"),
            Kind = MorphTypeKind.Particle,
            Name = new MultiString { { "en", "particle" } },
            Abbreviation = new MultiString { { "en", "part" } },
        },
        new()
        {
            Id = new Guid("d7f713db-e8cf-11d3-9764-00c04f186933"),
            Kind = MorphTypeKind.Prefix,
            Name = new MultiString { { "en", "prefix" } },
            Abbreviation = new MultiString { { "en", "pfx" } },
            Postfix = "-",
            SecondaryOrder = 20,
        },
        new()
        {
            Id = new Guid("d7f713e2-e8cf-11d3-9764-00c04f186933"),
            Kind = MorphTypeKind.Proclitic,
            Name = new MultiString { { "en", "proclitic" } },
            Abbreviation = new MultiString { { "en", "proclit" } },
            Postfix = "=",
            SecondaryOrder = 30,
        },
        new()
        {
            Id = new Guid("d7f713e5-e8cf-11d3-9764-00c04f186933"),
            Kind = MorphTypeKind.Root,
            Name = new MultiString { { "en", "root" } },
            Abbreviation = new MultiString { { "en", "ubd root" } },
        },
        new()
        {
            Id = new Guid("d7f713dc-e8cf-11d3-9764-00c04f186933"),
            Kind = MorphTypeKind.Simulfix,
            Name = new MultiString { { "en", "simulfix" } },
            Abbreviation = new MultiString { { "en", "smfx" } },
            Prefix = "=",
            Postfix = "=",
            SecondaryOrder = 60,
        },
        new()
        {
            Id = new Guid("d7f713e8-e8cf-11d3-9764-00c04f186933"),
            Kind = MorphTypeKind.Stem,
            Name = new MultiString { { "en", "stem" } },
            Abbreviation = new MultiString { { "en", "ubd stem" } },
        },
        new()
        {
            Id = new Guid("d7f713dd-e8cf-11d3-9764-00c04f186933"),
            Kind = MorphTypeKind.Suffix,
            Name = new MultiString { { "en", "suffix" } },
            Abbreviation = new MultiString { { "en", "sfx" } },
            Prefix = "-",
            SecondaryOrder = 70,
        },
        new()
        {
            Id = new Guid("d7f713de-e8cf-11d3-9764-00c04f186933"),
            Kind = MorphTypeKind.Suprafix,
            Name = new MultiString { { "en", "suprafix" } },
            Abbreviation = new MultiString { { "en", "spfx" } },
            Prefix = "~",
            Postfix = "~",
            SecondaryOrder = 50,
        },
        new()
        {
            Id = new Guid("18d9b1c3-b5b6-4c07-b92c-2fe1d2281bd4"),
            Kind = MorphTypeKind.InfixingInterfix,
            Name = new MultiString { { "en", "infixing interfix" } },
            Abbreviation = new MultiString { { "en", "ifxnfx" } },
            Prefix = "-",
            Postfix = "-",
        },
        new()
        {
            Id = new Guid("af6537b0-7175-4387-ba6a-36547d37fb13"),
            Kind = MorphTypeKind.PrefixingInterfix,
            Name = new MultiString { { "en", "prefixing interfix" } },
            Abbreviation = new MultiString { { "en", "pfxnfx" } },
            Postfix = "-",
        },
        new()
        {
            Id = new Guid("3433683d-08a9-4bae-ae53-2a7798f64068"),
            Kind = MorphTypeKind.SuffixingInterfix,
            Name = new MultiString { { "en", "suffixing interfix" } },
            Abbreviation = new MultiString { { "en", "sfxnfx" } },
            Prefix = "-",
        },
        new()
        {
            Id = new Guid("a23b6faa-1052-4f4d-984b-4b338bdaf95f"),
            Kind = MorphTypeKind.Phrase,
            Name = new MultiString { { "en", "phrase" } },
            Abbreviation = new MultiString { { "en", "phr" } },
        },
        new()
        {
            Id = new Guid("0cc8c35a-cee9-434d-be58-5d29130fba5b"),
            Kind = MorphTypeKind.DiscontiguousPhrase,
            Name = new MultiString { { "en", "discontiguous phrase" } },
            Abbreviation = new MultiString { { "en", "dis phr" } },
        },
    ];
}
