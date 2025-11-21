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

    internal static MorphType FromLcmMorphType(IMoMorphType? morphType)
    {
        var lcmMorphTypeId = morphType?.Id.Guid;

        return lcmMorphTypeId switch
        {
            null => MorphType.Unknown,
            // Can't switch on Guids since they're not compile-type constants, but thankfully pattern matching works
            Guid g when g == MoMorphTypeTags.kguidMorphBoundRoot => MorphType.BoundRoot,
            Guid g when g == MoMorphTypeTags.kguidMorphBoundStem => MorphType.BoundStem,
            Guid g when g == MoMorphTypeTags.kguidMorphCircumfix => MorphType.Circumfix,
            Guid g when g == MoMorphTypeTags.kguidMorphClitic => MorphType.Clitic,
            Guid g when g == MoMorphTypeTags.kguidMorphEnclitic => MorphType.Enclitic,
            Guid g when g == MoMorphTypeTags.kguidMorphInfix => MorphType.Infix,
            Guid g when g == MoMorphTypeTags.kguidMorphParticle => MorphType.Particle,
            Guid g when g == MoMorphTypeTags.kguidMorphPrefix => MorphType.Prefix,
            Guid g when g == MoMorphTypeTags.kguidMorphProclitic => MorphType.Proclitic,
            Guid g when g == MoMorphTypeTags.kguidMorphRoot => MorphType.Root,
            Guid g when g == MoMorphTypeTags.kguidMorphSimulfix => MorphType.Simulfix,
            Guid g when g == MoMorphTypeTags.kguidMorphStem => MorphType.Stem,
            Guid g when g == MoMorphTypeTags.kguidMorphSuffix => MorphType.Suffix,
            Guid g when g == MoMorphTypeTags.kguidMorphSuprafix => MorphType.Suprafix,
            Guid g when g == MoMorphTypeTags.kguidMorphInfixingInterfix => MorphType.InfixingInterfix,
            Guid g when g == MoMorphTypeTags.kguidMorphPrefixingInterfix => MorphType.PrefixingInterfix,
            Guid g when g == MoMorphTypeTags.kguidMorphSuffixingInterfix => MorphType.SuffixingInterfix,
            Guid g when g == MoMorphTypeTags.kguidMorphPhrase => MorphType.Phrase,
            Guid g when g == MoMorphTypeTags.kguidMorphDiscontiguousPhrase => MorphType.DiscontiguousPhrase,
            _ => MorphType.Other,
        };
    }

    internal static Guid? ToLcmMorphTypeId(MorphType morphType)
    {
        return morphType switch
        {
            MorphType.BoundRoot => MoMorphTypeTags.kguidMorphBoundRoot,
            MorphType.BoundStem => MoMorphTypeTags.kguidMorphBoundStem,
            MorphType.Circumfix => MoMorphTypeTags.kguidMorphCircumfix,
            MorphType.Clitic => MoMorphTypeTags.kguidMorphClitic,
            MorphType.Enclitic => MoMorphTypeTags.kguidMorphEnclitic,
            MorphType.Infix => MoMorphTypeTags.kguidMorphInfix,
            MorphType.Particle => MoMorphTypeTags.kguidMorphParticle,
            MorphType.Prefix => MoMorphTypeTags.kguidMorphPrefix,
            MorphType.Proclitic => MoMorphTypeTags.kguidMorphProclitic,
            MorphType.Root => MoMorphTypeTags.kguidMorphRoot,
            MorphType.Simulfix => MoMorphTypeTags.kguidMorphSimulfix,
            MorphType.Stem => MoMorphTypeTags.kguidMorphStem,
            MorphType.Suffix => MoMorphTypeTags.kguidMorphSuffix,
            MorphType.Suprafix => MoMorphTypeTags.kguidMorphSuprafix,
            MorphType.InfixingInterfix => MoMorphTypeTags.kguidMorphInfixingInterfix,
            MorphType.PrefixingInterfix => MoMorphTypeTags.kguidMorphPrefixingInterfix,
            MorphType.SuffixingInterfix => MoMorphTypeTags.kguidMorphSuffixingInterfix,
            MorphType.Phrase => MoMorphTypeTags.kguidMorphPhrase,
            MorphType.DiscontiguousPhrase => MoMorphTypeTags.kguidMorphDiscontiguousPhrase,
            MorphType.Unknown => null,
            MorphType.Other => null, // Note that this will not round-trip with FromLcmMorphType
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

    internal static IMoForm CreateLexemeForm(this LcmCache cache, MorphType morphType)
    {
        return
            IsAffixMorphType(morphType)
            ? cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create()
            : cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
    }

    internal static bool IsAffixMorphType(MorphType morphType)
    {
        return morphType switch
        {
            // Affixes of all types should use the Affix morph type factory
            MorphType.Circumfix => true,
            MorphType.Infix => true,
            MorphType.Prefix => true,
            MorphType.Simulfix => true,
            MorphType.Suffix => true,
            MorphType.Suprafix => true,
            MorphType.InfixingInterfix => true,
            MorphType.PrefixingInterfix => true,
            MorphType.SuffixingInterfix => true,

            // Everything else should use the Stem morph type factory
            _ => false,
        };
    }

    internal static ILexEntry CreateEntry(this LcmCache cache, Guid id, MorphType morphType)
    {
        var lexEntry = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(id,
            cache.ServiceLocator.GetInstance<ILangProjectRepository>().Singleton.LexDbOA);
        SetLexemeForm(lexEntry, morphType, cache);
        return lexEntry;
    }

    internal static IMoForm SetLexemeForm(ILexEntry lexEntry, MorphType morphType, LcmCache cache)
    {
        lexEntry.LexemeFormOA = cache.CreateLexemeForm(morphType);
        //must be done after the IMoForm is set on the LexemeForm property
        var lcmMorphType = ToLcmMorphTypeId(morphType) ?? ToLcmMorphTypeId(MorphType.Stem);
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
