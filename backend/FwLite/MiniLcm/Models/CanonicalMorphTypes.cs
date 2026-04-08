using System.Collections.Frozen;

namespace MiniLcm.Models;

/// <summary>
/// Canonical morph-type definitions copied from:
/// https://github.com/sillsdev/liblcm/blob/master/src/SIL.LCModel/Templates/NewLangProj.fwdata
/// </summary>
public static class CanonicalMorphTypes
{
    public static readonly FrozenDictionary<MorphTypeKind, MorphType> All = CreateAll().ToFrozenDictionary(m => m.Kind);

    private static MorphType[] CreateAll()
    {
        return
        [
            new()
            {
                Id = new Guid("d7f713e4-e8cf-11d3-9764-00c04f186933"),
                Kind = MorphTypeKind.BoundRoot,
                Name = new MultiString { { "en", "bound root" } },
                Abbreviation = new MultiString { { "en", "bd root" } },
                Description = new RichMultiString { { "en", new RichString("A bound root is a root which cannot occur as a separate word apart from any other morpheme.") } },
                Prefix = "*",
                SecondaryOrder = 10,
            },
            new()
            {
                Id = new Guid("d7f713e7-e8cf-11d3-9764-00c04f186933"),
                Kind = MorphTypeKind.BoundStem,
                Name = new MultiString { { "en", "bound stem" } },
                Abbreviation = new MultiString { { "en", "bd stem" } },
                // Do not correct the doubled space in the next line
                Description = new RichMultiString { { "en", new RichString("A bound stem is a stem  which cannot occur as a separate word apart from any other morpheme.") } },
                Prefix = "*",
                SecondaryOrder = 10,
            },
            new()
            {
                Id = new Guid("d7f713df-e8cf-11d3-9764-00c04f186933"),
                Kind = MorphTypeKind.Circumfix,
                Name = new MultiString { { "en", "circumfix" } },
                Abbreviation = new MultiString { { "en", "cfx" } },
                Description = new RichMultiString { { "en", new RichString("A circumfix is an affix made up of two separate parts which surround and attach to a root or stem.") } },
                SecondaryOrder = 0,
            },
            new()
            {
                Id = new Guid("c2d140e5-7ca9-41f4-a69a-22fc7049dd2c"),
                Kind = MorphTypeKind.Clitic,
                Name = new MultiString { { "en", "clitic" } },
                Abbreviation = new MultiString { { "en", "clit" } },
                Description = new RichMultiString { { "en", new RichString("A clitic is a morpheme that has syntactic characteristics of a word, but shows evidence of being phonologically bound to another word. Orthographically, it stands alone.") } },
                SecondaryOrder = 0,
            },
            new()
            {
                Id = new Guid("d7f713e1-e8cf-11d3-9764-00c04f186933"),
                Kind = MorphTypeKind.Enclitic,
                Name = new MultiString { { "en", "enclitic" } },
                Abbreviation = new MultiString { { "en", "enclit" } },
                Description = new RichMultiString { { "en", new RichString("An enclitic is a clitic that is phonologically joined at the end of a preceding word to form a single unit.") } },
                Prefix = "=",
                SecondaryOrder = 80,
            },
            new()
            {
                Id = new Guid("d7f713da-e8cf-11d3-9764-00c04f186933"),
                Kind = MorphTypeKind.Infix,
                Name = new MultiString { { "en", "infix" } },
                Abbreviation = new MultiString { { "en", "ifx" } },
                Description = new RichMultiString { { "en", new RichString("An infix is an affix that is inserted within a root or stem.") } },
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
                Description = new RichMultiString { { "en", new RichString("A particle is a word that does not belong to one of the main classes of words, is invariable in form, and typically has grammatical or pragmatic meaning.") } },
                SecondaryOrder = 0,
            },
            new()
            {
                Id = new Guid("d7f713db-e8cf-11d3-9764-00c04f186933"),
                Kind = MorphTypeKind.Prefix,
                Name = new MultiString { { "en", "prefix" } },
                Abbreviation = new MultiString { { "en", "pfx" } },
                Description = new RichMultiString { { "en", new RichString("A prefix is an affix that is joined before a root or stem.") } },
                Postfix = "-",
                SecondaryOrder = 20,
            },
            new()
            {
                Id = new Guid("d7f713e2-e8cf-11d3-9764-00c04f186933"),
                Kind = MorphTypeKind.Proclitic,
                Name = new MultiString { { "en", "proclitic" } },
                Abbreviation = new MultiString { { "en", "proclit" } },
                Description = new RichMultiString { { "en", new RichString("A proclitic is a clitic that precedes the word to which it is phonologically joined.") } },
                Postfix = "=",
                SecondaryOrder = 30,
            },
            new()
            {
                Id = new Guid("d7f713e5-e8cf-11d3-9764-00c04f186933"),
                Kind = MorphTypeKind.Root,
                Name = new MultiString { { "en", "root" } },
                Abbreviation = new MultiString { { "en", "ubd root" } },
                Description = new RichMultiString { { "en", new RichString("A root is the portion of a word that (i) is common to a set of derived or inflected forms, if any, when all affixes are removed, (ii) is not further analyzable into meaningful elements, being morphologically simple, and, (iii) carries the principle portion of meaning of the words in which it functions.") } },
                SecondaryOrder = 0,
            },
            new()
            {
                Id = new Guid("d7f713dc-e8cf-11d3-9764-00c04f186933"),
                Kind = MorphTypeKind.Simulfix,
                Name = new MultiString { { "en", "simulfix" } },
                Abbreviation = new MultiString { { "en", "smfx" } },
                Description = new RichMultiString { { "en", new RichString("A simulfix is a change or replacement of vowels or consonants (usually vowels) which changes the meaning of a word.  (Note: the parser does not currently handle simulfixes.)") } },
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
                Description = new RichMultiString { { "en", new RichString("A stem is the root or roots of a word, together with any derivational affixes, to which inflectional affixes are added.\" (LinguaLinks Library).  A stem \"may consist solely of a single root morpheme (i.e. a 'simple' stem as in 'man'), or of two root morphemes (e.g. a 'compound' stem, as in 'blackbird'), or of a root morpheme plus a derivational affix (i.e. a 'complex' stem, as in 'manly', 'unmanly', 'manliness').  All have in common the notion that it is to the stem that inflectional affixes are attached.\" (Crystal, 1997:362)") } },
                SecondaryOrder = 0,
            },
            new()
            {
                Id = new Guid("d7f713dd-e8cf-11d3-9764-00c04f186933"),
                Kind = MorphTypeKind.Suffix,
                Name = new MultiString { { "en", "suffix" } },
                Abbreviation = new MultiString { { "en", "sfx" } },
                Description = new RichMultiString { { "en", new RichString("A suffix is an affix that is attached to the end of a root or stem.") } },
                Prefix = "-",
                SecondaryOrder = 70,
            },
            new()
            {
                Id = new Guid("d7f713de-e8cf-11d3-9764-00c04f186933"),
                Kind = MorphTypeKind.Suprafix,
                Name = new MultiString { { "en", "suprafix" } },
                Abbreviation = new MultiString { { "en", "spfx" } },
                // Do not correct the doubled space in the next line
                Description = new RichMultiString { { "en", new RichString("A suprafix is a kind of affix in which a suprasegmental is superimposed on one or more syllables of the root or stem, signalling a particular  morphosyntactic operation.  (Note: the parser does not currently handle suprafixes.)") } },
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
                Description = new RichMultiString { { "en", new RichString("An infixing interfix is an infix that can occur between two roots or stems.") } },
                Prefix = "-",
                Postfix = "-",
                SecondaryOrder = 0,
            },
            new()
            {
                Id = new Guid("af6537b0-7175-4387-ba6a-36547d37fb13"),
                Kind = MorphTypeKind.PrefixingInterfix,
                Name = new MultiString { { "en", "prefixing interfix" } },
                Abbreviation = new MultiString { { "en", "pfxnfx" } },
                Description = new RichMultiString { { "en", new RichString("A prefixing interfix is a prefix that can occur between two roots or stems.") } },
                Postfix = "-",
                SecondaryOrder = 0,
            },
            new()
            {
                Id = new Guid("3433683d-08a9-4bae-ae53-2a7798f64068"),
                Kind = MorphTypeKind.SuffixingInterfix,
                Name = new MultiString { { "en", "suffixing interfix" } },
                Abbreviation = new MultiString { { "en", "sfxnfx" } },
                Description = new RichMultiString { { "en", new RichString("A suffixing interfix is an suffix that can occur between two roots or stems.") } },
                Prefix = "-",
                SecondaryOrder = 0,
            },
            new()
            {
                Id = new Guid("a23b6faa-1052-4f4d-984b-4b338bdaf95f"),
                Kind = MorphTypeKind.Phrase,
                Name = new MultiString { { "en", "phrase" } },
                Abbreviation = new MultiString { { "en", "phr" } },
                Description = new RichMultiString { { "en", new RichString("A phrase is a syntactic structure that consists of more than one word but lacks the subject-predicate organization of a clause.") } },
                SecondaryOrder = 0,
            },
            new()
            {
                Id = new Guid("0cc8c35a-cee9-434d-be58-5d29130fba5b"),
                Kind = MorphTypeKind.DiscontiguousPhrase,
                Name = new MultiString { { "en", "discontiguous phrase" } },
                Abbreviation = new MultiString { { "en", "dis phr" } },
                Description = new RichMultiString { { "en", new RichString("A discontiguous phrase has discontiguous constituents which (a) are separated from each other by one or more intervening constituents, and (b) are considered either (i) syntactically contiguous and unitary, or (ii) realizing the same, single meaning. An example is French ne...pas.") } },
                SecondaryOrder = 0,
            },
        ];
    }
}
