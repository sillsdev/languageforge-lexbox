using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FwDataMiniLcmBridge.Api;
using Gridify;
using Microsoft.Win32;
using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge;

public class FwDataBridgeConfig
{
    //used to allow comparing null to an empty list, eg Senses=null should be true when there are no senses
    private static object? EmptyToNull<T>(IList<T> list) => list.Count == 0 ? null : list;
    private static object? EmptyToNull<T>(IEnumerable<T> list) => !list.Any() ? null : list;
    public FwDataBridgeConfig()
    {
        Mapper.Configuration.DisableCollectionNullChecks = true;
        Mapper.AddMap(nameof(Entry.ComplexFormTypes),
            e => e.ComplexFormEntryRefs.SingleOrDefault() == null
                ? null
                : EmptyToNull(e.ComplexFormEntryRefs.Single().ComplexEntryTypesRS));
        Mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.SemanticDomains)}", e => e.AllSenses.Select(s => EmptyToNull(s.SemanticDomainsRC)));
        Mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.ExampleSentences)}", e => EmptyToNull(e.AllSenses.SelectMany(s => s.ExamplesOS)));
        Mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.PartOfSpeechId)}",
            e => e.AllSenses.Select(s => s.MorphoSyntaxAnalysisRA == null
                ? null
                : s.MorphoSyntaxAnalysisRA.GetPartOfSpeechId()));

        Mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.Gloss)}",
            (entry, key) => entry.AllSenses.Select(s => s.PickText(s.Gloss, key)));
        Mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.Definition)}",
            (entry, key) => entry.AllSenses.Select(s => s.PickText(s.Definition, key)));
        Mapper.AddMap(nameof(Entry.Senses), e => EmptyToNull(e.AllSenses));
        Mapper.AddMap(nameof(Entry.LexemeForm), (entry, key) => entry.PickText(entry.LexemeFormOA.Form, key));
        Mapper.AddMap(nameof(Entry.CitationForm), (entry, key) => entry.PickText(entry.CitationForm, key));
        Mapper.AddMap(nameof(Entry.Note), (entry, key) => entry.PickText(entry.Comment, key));
    }

    private static string UnixDataFolder =>
        Environment.GetEnvironmentVariable("XDG_DATA_HOME") ??
        Path.Join(Environment.GetEnvironmentVariable("HOME") ?? "", ".local", "share");

    private static string UnixProgramFolder => UnixDataFolder;

    [SupportedOSPlatform("windows")]
    private static string WindowsDataFolder =>
        (string?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\SIL\FieldWorks\9", "ProjectsDir", null)
        ?? (string?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\SIL\FieldWorks\9", "ProjectsDir", null)
        ?? @"C:\ProgramData\SIL\FieldWorks\Projects";

    [SupportedOSPlatform("windows")]
    private static string WindowsProgramFolder =>
        (string?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\SIL\FieldWorks\9", "RootCodeDir", null)
        ?? (string?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\SIL\FieldWorks\9", "RootCodeDir", null)
        ?? @"C:\Program Files\SIL\FieldWorks 9";

    private static readonly string DataFolder =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? WindowsDataFolder
            : Path.Join(UnixDataFolder, "fieldworks", "Projects");

    private static readonly string ProgramFolder =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? WindowsProgramFolder
            : Path.Join(UnixProgramFolder, "fieldworks");

    public string ProjectsFolder { get; set; } = DataFolder;
    public string TemplatesFolder { get; set; } = Path.Join(ProgramFolder, "Templates");

    public GridifyMapper<ILexEntry> Mapper { get; set; } = new GridifyMapper<ILexEntry>(false);
}
