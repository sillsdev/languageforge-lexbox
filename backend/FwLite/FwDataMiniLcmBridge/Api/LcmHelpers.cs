using System.Globalization;
using MiniLcm.Culture;
using MiniLcm.Models;
using SIL.Extensions;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;

namespace FwDataMiniLcmBridge.Api;

internal static class LcmHelpers
{
    internal static string? LexEntryHeadword(this ILexEntry entry, int? ws = null)
    {
        var citationFormTs =
            ws.HasValue ? entry.CitationForm.get_String(ws.Value)
            : entry.CitationForm.StringCount > 0 ? entry.CitationForm.BestVernacularAlternative
            : null;
        var citationForm = citationFormTs?.Text?.Trim(WhitespaceChars);

        if (!string.IsNullOrEmpty(citationForm)) return citationForm;

        var lexemeFormTs =
            ws.HasValue ? entry.LexemeFormOA?.Form.get_String(ws.Value)
            : entry.LexemeFormOA?.Form.StringCount > 0 ? entry.LexemeFormOA?.Form.BestVernacularAlternative
            : null;
        var lexemeForm = lexemeFormTs?.Text?.Trim(WhitespaceChars);

        return lexemeForm;
    }

    internal static string LexEntryHeadwordOrUnknown(this ILexEntry entry, int? ws = null)
    {
        var headword = entry.LexEntryHeadword(ws);
        return string.IsNullOrEmpty(headword) ? Entry.UnknownHeadword : headword;
    }

    internal static bool SearchValue(this ITsMultiString multiString, string value)
    {
        for (var i = 0; i < multiString.StringCount; i++)
        {
            var tsString = multiString.GetStringFromIndex(i, out var _);
            if (string.IsNullOrEmpty(tsString.Text)) continue;
            if (tsString.Text.ContainsDiacriticMatch(value))
            {
                return true;
            }
        }
        return false;
    }

    internal static readonly char[] WhitespaceChars =
    [
        '\u0009', // Tab
        '\u000A', // Line Feed
        '\u000D', // Carriage Return
        '\u0020', // Space
        '\u00A0', // Non-breaking Space
        '\u2002', // En Space
        '\u2003', // Em Space
        '\u2004', // Three-Per-Em Space
        '\u2005', // Four-Per-Em Space
        '\u2006', // Six-Per-Em Space
        '\u2007', // Figure Space
        '\u2008', // Punctuation Space
        '\u2009', // Thin Space
        '\u200A', // Hair Space
        '\u200B', // Zero Width Space
        '\u200C', // Zero Width Non-Joiner
        '\u200D', // Zero Width Joiner
        '\u200E', // Left-to-Right Mark
        '\u200F',  // Right-to-Left Mark
        '\u2028', // Line Separator
        '\u2029', // Paragraph Separator
        '\u202F', // Narrow No-Break Space
        '\u205F', // Medium Mathematical Space
        '\u3000',  // Ideographic Space
        '\uFEFF', // Zero Width No-Break Space / BOM
    ];

    internal static readonly char[] WhitespaceAndFormattingChars =
    [
        .. WhitespaceChars,
        '\u0640', // Arabic Tatweel
    ];

    internal static MorphTypeKind FromLcmMorphType(IMoMorphType? morphType)
    {
        var lcmMorphTypeId = morphType?.Id.Guid;

        return lcmMorphTypeId switch
        {
            null => MorphTypeKind.Unknown,
            // Can't switch on Guids since they're not compile-type constants, but thankfully pattern matching works
            Guid g when g == MoMorphTypeTags.kguidMorphBoundRoot => MorphTypeKind.BoundRoot,
            Guid g when g == MoMorphTypeTags.kguidMorphBoundStem => MorphTypeKind.BoundStem,
            Guid g when g == MoMorphTypeTags.kguidMorphCircumfix => MorphTypeKind.Circumfix,
            Guid g when g == MoMorphTypeTags.kguidMorphClitic => MorphTypeKind.Clitic,
            Guid g when g == MoMorphTypeTags.kguidMorphEnclitic => MorphTypeKind.Enclitic,
            Guid g when g == MoMorphTypeTags.kguidMorphInfix => MorphTypeKind.Infix,
            Guid g when g == MoMorphTypeTags.kguidMorphParticle => MorphTypeKind.Particle,
            Guid g when g == MoMorphTypeTags.kguidMorphPrefix => MorphTypeKind.Prefix,
            Guid g when g == MoMorphTypeTags.kguidMorphProclitic => MorphTypeKind.Proclitic,
            Guid g when g == MoMorphTypeTags.kguidMorphRoot => MorphTypeKind.Root,
            Guid g when g == MoMorphTypeTags.kguidMorphSimulfix => MorphTypeKind.Simulfix,
            Guid g when g == MoMorphTypeTags.kguidMorphStem => MorphTypeKind.Stem,
            Guid g when g == MoMorphTypeTags.kguidMorphSuffix => MorphTypeKind.Suffix,
            Guid g when g == MoMorphTypeTags.kguidMorphSuprafix => MorphTypeKind.Suprafix,
            Guid g when g == MoMorphTypeTags.kguidMorphInfixingInterfix => MorphTypeKind.InfixingInterfix,
            Guid g when g == MoMorphTypeTags.kguidMorphPrefixingInterfix => MorphTypeKind.PrefixingInterfix,
            Guid g when g == MoMorphTypeTags.kguidMorphSuffixingInterfix => MorphTypeKind.SuffixingInterfix,
            Guid g when g == MoMorphTypeTags.kguidMorphPhrase => MorphTypeKind.Phrase,
            Guid g when g == MoMorphTypeTags.kguidMorphDiscontiguousPhrase => MorphTypeKind.DiscontiguousPhrase,
            // Non-canonical Guids should not be guessed, but be mapped to Unknown
            _ => MorphTypeKind.Unknown,
        };
    }

    internal static Guid? ToLcmMorphTypeId(MorphTypeKind morphType)
    {
        return morphType switch
        {
            MorphTypeKind.BoundRoot => MoMorphTypeTags.kguidMorphBoundRoot,
            MorphTypeKind.BoundStem => MoMorphTypeTags.kguidMorphBoundStem,
            MorphTypeKind.Circumfix => MoMorphTypeTags.kguidMorphCircumfix,
            MorphTypeKind.Clitic => MoMorphTypeTags.kguidMorphClitic,
            MorphTypeKind.Enclitic => MoMorphTypeTags.kguidMorphEnclitic,
            MorphTypeKind.Infix => MoMorphTypeTags.kguidMorphInfix,
            MorphTypeKind.Particle => MoMorphTypeTags.kguidMorphParticle,
            MorphTypeKind.Prefix => MoMorphTypeTags.kguidMorphPrefix,
            MorphTypeKind.Proclitic => MoMorphTypeTags.kguidMorphProclitic,
            MorphTypeKind.Root => MoMorphTypeTags.kguidMorphRoot,
            MorphTypeKind.Simulfix => MoMorphTypeTags.kguidMorphSimulfix,
            MorphTypeKind.Stem => MoMorphTypeTags.kguidMorphStem,
            MorphTypeKind.Suffix => MoMorphTypeTags.kguidMorphSuffix,
            MorphTypeKind.Suprafix => MoMorphTypeTags.kguidMorphSuprafix,
            MorphTypeKind.InfixingInterfix => MoMorphTypeTags.kguidMorphInfixingInterfix,
            MorphTypeKind.PrefixingInterfix => MoMorphTypeTags.kguidMorphPrefixingInterfix,
            MorphTypeKind.SuffixingInterfix => MoMorphTypeTags.kguidMorphSuffixingInterfix,
            MorphTypeKind.Phrase => MoMorphTypeTags.kguidMorphPhrase,
            MorphTypeKind.DiscontiguousPhrase => MoMorphTypeTags.kguidMorphDiscontiguousPhrase,
            MorphTypeKind.Unknown => null,
            _ => null,
        };
    }

    internal static void ContributeExemplars(ITsMultiString multiString, IReadOnlyDictionary<int, HashSet<char>> wsExemplars)
    {
        for (var i = 0; i < multiString.StringCount; i++)
        {
            var tsString = multiString.GetStringFromIndex(i, out var ws);
            if (string.IsNullOrEmpty(tsString.Text)) continue;
            var value = tsString.Text.AsSpan().Trim(WhitespaceAndFormattingChars);
            if (!value.IsEmpty && wsExemplars.TryGetValue(ws, out var exemplars))
            {
                //some cases the first character is not a prefix.
                if (CultureInfo.InvariantCulture.CompareInfo.IsPrefix(value, value[0..1], CompareOptions.IgnoreCase))
                    exemplars.Add(char.ToUpperInvariant(value[0]));
            }
        }
    }

    internal static WritingSystemId GetWritingSystemId(this LcmCache cache, int ws)
    {
        return cache.ServiceLocator.WritingSystemManager.Get(ws).Id;
    }

    internal static CoreWritingSystemDefinition? TryGetCoreWritingSystem(this LcmCache cache, WritingSystemId wsId, WritingSystemType type)
    {
        var wsContainer = cache.ServiceLocator.WritingSystems;
        var writingSystemsOfType = type switch
        {
            WritingSystemType.Analysis => wsContainer.AnalysisWritingSystems,
            WritingSystemType.Vernacular => wsContainer.VernacularWritingSystems,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        if (wsId == default) return writingSystemsOfType.FirstOrDefault();
        return writingSystemsOfType.FirstOrDefault(ws => ws.Id == wsId);
    }

    internal static CoreWritingSystemDefinition GetCoreWritingSystem(this LcmCache cache, WritingSystemId wsId, WritingSystemType? type = null)
    {
        if (type is not null)
        {
            return TryGetCoreWritingSystem(cache, wsId, type.Value)
                ?? throw new NullReferenceException($"unable to find writing system with id '{wsId.Code}' of type {type}");
        }

        if (wsId == default)
        {
            throw new ArgumentException("Cannot look up default writing system ID when type is not specified", nameof(wsId));
        }

        if (!cache.ServiceLocator.WritingSystemManager.TryGet(wsId.Code, out var lcmWs))
        {
            throw new NullReferenceException($"unable to find writing system with id '{wsId.Code}'");
        }

        return lcmWs ?? throw new NullReferenceException($"unable to find writing system with id {wsId}");
    }

    internal static int GetWritingSystemHandle(this LcmCache cache, WritingSystemId wsId, WritingSystemType? type = null)
    {
        var ws = GetCoreWritingSystem(cache, wsId, type);
        return ws.Handle;
    }

    internal static string PickText(this ICmObject obj, ITsMultiString multiString, string ws)
    {
        var wsHandle = obj.Cache.GetWritingSystemHandle(ws);
        return multiString.get_String(wsHandle)?.Text ?? string.Empty;
    }

    internal static IMoForm CreateLexemeForm(this LcmCache cache, MorphTypeKind morphType)
    {
        return
            IsAffixMorphType(morphType)
            ? cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create()
            : cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
    }

    internal static bool IsAffixMorphType(MorphTypeKind morphType)
    {
        return morphType switch
        {
            // Affixes of all types should use the Affix morph type factory
            MorphTypeKind.Circumfix => true,
            MorphTypeKind.Infix => true,
            MorphTypeKind.Prefix => true,
            MorphTypeKind.Simulfix => true,
            MorphTypeKind.Suffix => true,
            MorphTypeKind.Suprafix => true,
            MorphTypeKind.InfixingInterfix => true,
            MorphTypeKind.PrefixingInterfix => true,
            MorphTypeKind.SuffixingInterfix => true,

            // Everything else should use the Stem morph type factory
            _ => false,
        };
    }

    internal static ILexEntry CreateEntry(this LcmCache cache, Guid id, MorphTypeKind morphType)
    {
        var lexEntry = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(id,
            cache.ServiceLocator.GetInstance<ILangProjectRepository>().Singleton.LexDbOA);
        SetLexemeForm(lexEntry, morphType, cache);
        return lexEntry;
    }

    internal static IMoForm SetLexemeForm(ILexEntry lexEntry, MorphTypeKind morphType, LcmCache cache)
    {
        lexEntry.LexemeFormOA = cache.CreateLexemeForm(morphType);
        //must be done after the IMoForm is set on the LexemeForm property
        var lcmMorphType = ToLcmMorphTypeId(morphType) ?? ToLcmMorphTypeId(MorphTypeKind.Stem);
        lexEntry.LexemeFormOA.MorphTypeRA = cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(lcmMorphType!.Value);
        return lexEntry.LexemeFormOA;
    }

    internal static string GetSemanticDomainCode(ICmSemanticDomain semanticDomain)
    {
        var abbr = semanticDomain.Abbreviation;
        // UiString can be null even though there is an abbreviation available
        return abbr.UiString ?? abbr.BestVernacularAnalysisAlternative.Text;
    }

    internal static void SetString(this ITsMultiString multiString, FwDataMiniLcmApi api, WritingSystemId ws, string value)
    {
        var writingSystemHandle = api.GetWritingSystemHandle(ws);
        if (!ws.IsAudio)
        {
            multiString.set_String(writingSystemHandle, TsStringUtils.MakeString(value, writingSystemHandle));
        }
        else
        {
            var tsString = TsStringUtils.MakeString(api.FromMediaUri(value),
                writingSystemHandle
            );
            multiString.set_String(writingSystemHandle, tsString);
        }
    }
    internal static void SetString(this ITsMultiString multiString, FwDataMiniLcmApi api, WritingSystemId ws, RichString value)
    {
        var writingSystemHandle = api.GetWritingSystemHandle(ws);
        if (!ws.IsAudio)
        {
            multiString.set_String(writingSystemHandle,
                RichTextMapping.ToTsString(value, id => api.GetWritingSystemHandle(id)));
        }
        else
        {
            var tsString = TsStringUtils.MakeString(api.FromMediaUri(value.GetPlainText()), writingSystemHandle);
            multiString.set_String(writingSystemHandle, tsString);
        }
    }

    //mostly a copy of method in SIL.FieldWorks.FwCoreDlgs.FwWritingSystemSetupModel
    internal static void AddOrMoveInList(
        ICollection<CoreWritingSystemDefinition> allWritingSystems,
        int desiredIndex,
        CoreWritingSystemDefinition workingWs
    )
    {
        if (desiredIndex < 0) throw new ArgumentOutOfRangeException(nameof(desiredIndex), desiredIndex, "desiredIndex must be >= 0");

        // copy original contents into a list
        var updatedList = new List<CoreWritingSystemDefinition>(allWritingSystems);
        var ws = updatedList.Find(listItem =>
        {
            if (ReferenceEquals(listItem, workingWs)) return true;
            var workingTag = string.IsNullOrEmpty(workingWs.Id) ? workingWs.LanguageTag : workingWs.Id;
            var listItemTag = string.IsNullOrEmpty(listItem.Id) ? listItem.LanguageTag : listItem.Id;
            return string.Equals(listItemTag, workingTag);
        });

        if (ws != null)
        {
            updatedList.Remove(ws);
        }

        if (desiredIndex > updatedList.Count)
        {
            updatedList.Add(workingWs);
        }
        else
        {
            updatedList.Insert(desiredIndex, workingWs);
        }

        allWritingSystems.Clear();
        allWritingSystems.AddRange(updatedList);
    }
}
