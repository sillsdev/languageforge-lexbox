using System.Globalization;
using MiniLcm.Culture;
using MiniLcm.Models;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace FwDataMiniLcmBridge.Api;

internal static class LcmHelpers
{
    internal static string? LexEntryHeadword(this ILexEntry entry, int? ws = null)
    {
        var citationFormTs =
            ws.HasValue ? entry.CitationForm.get_String(ws.Value)
            : entry.CitationForm.StringCount > 0 ? entry.CitationForm.GetStringFromIndex(0, out var _)
            : null;
        var citationForm = citationFormTs?.Text?.Trim(WhitespaceChars);

        if (!string.IsNullOrEmpty(citationForm)) return citationForm;

        var lexemeFormTs =
            ws.HasValue ? entry.LexemeFormOA?.Form.get_String(ws.Value)
            : entry.LexemeFormOA?.Form.StringCount > 0 ? entry.LexemeFormOA?.Form.GetStringFromIndex(0, out var _)
            : null;
        var lexemeForm = lexemeFormTs?.Text?.Trim(WhitespaceChars);

        return lexemeForm;
    }

    internal static string? LexEntryHeadwordOrUnknown(this ILexEntry entry, int? ws = null)
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

    internal static int GetWritingSystemHandle(this LcmCache cache, WritingSystemId ws, WritingSystemType? type = null)
    {
        var wsContainer = cache.ServiceLocator.WritingSystems;
        if (ws == "default")
        {
            return type switch
            {
                WritingSystemType.Analysis => wsContainer.DefaultAnalysisWritingSystem.Handle,
                WritingSystemType.Vernacular => wsContainer.DefaultVernacularWritingSystem.Handle,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        if (!cache.ServiceLocator.WritingSystemManager.TryGet(ws.Code, out var lcmWs))
        {
            throw new NullReferenceException($"unable to find writing system with id '{ws.Code}'");
        }
        if (lcmWs is not null && type is not null)
        {
            var validWs = type switch
            {
                WritingSystemType.Analysis => wsContainer.AnalysisWritingSystems,
                WritingSystemType.Vernacular => wsContainer.VernacularWritingSystems,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            if (!validWs.Contains(lcmWs))
            {
                throw new InvalidOperationException($"Writing system {ws} is not of the requested type: {type}.");
            }
        }
        if (lcmWs is null)
        {
            throw new NullReferenceException($"unable to find writing system with id {ws}");
        }

        return lcmWs.Handle;
    }

    internal static string? PickText(this ICmObject obj, ITsMultiString multiString, string ws)
    {
        var wsHandle = obj.Cache.GetWritingSystemHandle(ws);
        return multiString.get_String(wsHandle)?.Text ?? "";
    }

    internal static IMoStemAllomorph CreateLexemeForm(this LcmCache cache)
    {
        return cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
    }

    internal static ILexEntry CreateEntry(this LcmCache cache, Guid id)
    {
        var lexEntry = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(id,
            cache.ServiceLocator.GetInstance<ILangProjectRepository>().Singleton.LexDbOA);
        lexEntry.LexemeFormOA = cache.CreateLexemeForm();
        //must be done after the IMoForm is set on the LexemeForm property
        lexEntry.LexemeFormOA.MorphTypeRA = cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
        return lexEntry;
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
}
