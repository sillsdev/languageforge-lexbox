using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Utils;
using SIL.WritingSystems;

namespace FwDataMiniLcmBridge.LcmUtils;

public interface IProjectLoader
{
    /// <summary>
    /// loads a fwdata file that lives in the project folder C:\ProgramData\SIL\FieldWorks\Projects
    /// </summary>
    /// <param name="fileName">could be the full path or just the file name, the path will be ignored, must include the extension</param>
    /// <param name="projectFolder">folder to load the project from instead of C:\ProgramData\SIL\FieldWorks\Projects</param>
    /// <returns></returns>
    LcmCache LoadCache(FwDataProject project);

    LcmCache NewProject(FwDataProject project, string analysisWs, string vernacularWs, string uiWs = "en");

    /// <summary>
    /// Like the single-WS overload, but the new project also gets every analysis/vernacular writing
    /// system in the given lists. The first item of each list is the default of that type.
    /// </summary>
    LcmCache NewProject(FwDataProject project, IReadOnlyList<string> analysisWss, IReadOnlyList<string> vernacularWss, string uiWs = "en");
}

public class ProjectLoader(IOptions<FwDataBridgeConfig> config) : IProjectLoader
{
    protected string TemplatesFolder => config.Value.TemplatesFolder;
    private static bool _init;
    private static readonly object _initLock = new();

    public static void Init()
    {
        if (_init)
        {
            return;
        }

        lock (_initLock)
        {
            if (_init)
            {
                return;
            }

            Icu.Wrapper.Init();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Debug.Assert(Icu.Wrapper.IcuVersion == "72.1.0.3");
            }

            Sldr.Initialize();
            _init = true;
        }
    }

    public virtual LcmCache LoadCache(FwDataProject project)
    {
        using var activity = FwDataMiniLcmBridgeActivitySource.Value.StartActivity();
        activity?.SetTag("app.project_code", project.Name);
        Init();
        if (!Directory.Exists(project.ProjectsPath)) Directory.CreateDirectory(project.ProjectsPath);
        if (!Directory.Exists(TemplatesFolder)) Directory.CreateDirectory(TemplatesFolder);
        var lcmDirectories = new LcmDirectories(project.ProjectsPath, TemplatesFolder);
        var progress = new LcmThreadedProgress();
        var cache = LcmCache.CreateCacheFromLocalProjectFile(project.FilePath,
            null,
            new LfLcmUi(progress.SynchronizeInvoke),
            lcmDirectories,
            new LcmSettings()
            {
                DisableDataMigration = !config.Value.AutoMigrateLcmData
            },
            progress
        );
        return cache;
    }

    public virtual LcmCache NewProject(FwDataProject project, string analysisWs, string vernacularWs, string uiWs = "en")
    {
        return NewProject(project, [analysisWs], [vernacularWs], uiWs);
    }

    public virtual LcmCache NewProject(FwDataProject project,
        IReadOnlyList<string> analysisWss,
        IReadOnlyList<string> vernacularWss,
        string uiWs = "en")
    {
        Init();
        var lcmDirectories = new LcmDirectories(project.ProjectsPath, TemplatesFolder);
        var progress = new LcmThreadedProgress();
        var analysisDefinitions = analysisWss.Select(CreateWritingSystemDefinition).ToList();
        var vernacularDefinitions = vernacularWss.Select(CreateWritingSystemDefinition).ToList();
        // A liblcm bug: when no anthropology list file is passed (FLEx passes one unless the user picks an
        // empty list), CreateNewLangProj names the empty list in the default analysis WS, which it looks up
        // with GetWsFromStr(IcuLocale). GetWsFromStr expects a language tag, so this only works when the
        // two match: "pt-BR" is "pt_BR" in ICU, the lookup returns 0, and creation throws with the project
        // already on disk. Remove this workaround once https://github.com/sillsdev/liblcm/issues/416 is fixed and released.
        // Until then hand liblcm "en" as the default
        // and pass every requested analysis WS as additional; SetRequestedWritingSystems fixes the default.
        var substituteDefaultAnalysis = !IcuLocaleMatchesTag(analysisDefinitions[0]);
        var defaultAnalysis = substituteDefaultAnalysis ? CreateWritingSystemDefinition("en") : analysisDefinitions[0];
        NewProject(progress,
            project.Name,
            lcmDirectories,
            progress.SynchronizeInvoke,
            defaultAnalysis,
            vernacularDefinitions[0],
            uiWs,
            AdditionalWritingSystems(analysisDefinitions, defaultAnalysis),
            AdditionalWritingSystems(vernacularDefinitions, vernacularDefinitions[0]));
        var cache = LoadCache(project);
        try
        {
            SetRequestedWritingSystems(cache, analysisWss, vernacularWss, substituteDefaultAnalysis);
        }
        catch
        {
            cache.Dispose();
            throw;
        }
        return cache;
    }

    private static CoreWritingSystemDefinition CreateWritingSystemDefinition(string ws) => new(ws) { Id = ws };

    private static bool IcuLocaleMatchesTag(CoreWritingSystemDefinition definition) =>
        string.Equals(definition.IcuLocale, definition.Id, StringComparison.OrdinalIgnoreCase);

    // Everything except the default, which CreateNewLangProj takes on its own
    private static HashSet<CoreWritingSystemDefinition> AdditionalWritingSystems(
        List<CoreWritingSystemDefinition> definitions,
        CoreWritingSystemDefinition defaultDefinition) =>
        definitions.Where(d => !string.Equals(d.Id, defaultDefinition.Id, StringComparison.OrdinalIgnoreCase)).ToHashSet();

    /// <summary>
    /// Makes the current analysis and vernacular lists exactly the requested writing systems, in the
    /// requested order. CreateNewLangProj doesn't on its own: it adds "en" as a current analysis WS
    /// whenever another one is the default, appends the "additional" sets without checking for one that's
    /// already there (so "en" listed later, or "fr" plus "fr-Latn", end up twice), and takes them as
    /// unordered sets. Writing systems it added that weren't asked for stay in the full lists, unselected.
    /// </summary>
    private static void SetRequestedWritingSystems(LcmCache cache,
        IReadOnlyList<string> analysisWss,
        IReadOnlyList<string> vernacularWss,
        bool substitutedDefaultAnalysis)
    {
        var container = cache.ServiceLocator.WritingSystems;
        var wsManager = cache.ServiceLocator.WritingSystemManager;
        // GetOrSet resolves a tag to the WS liblcm already created for it, also when liblcm stored it under
        // an equivalent tag (e.g. "fr" for "fr-Latn")
        List<CoreWritingSystemDefinition> Resolve(IReadOnlyList<string> tags) => tags.Select(tag =>
        {
            wsManager.GetOrSet(tag, out var ws);
            return ws;
        }).Distinct().ToList();

        NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
        {
            var analysis = Resolve(analysisWss);
            var vernacular = Resolve(vernacularWss);
            // liblcm doesn't keep the full lists in the same order as the current lists, so set both
            SetList(container.AnalysisWritingSystems, [.. analysis, .. container.AnalysisWritingSystems.Except(analysis)]);
            SetList(container.VernacularWritingSystems, [.. vernacular, .. container.VernacularWritingSystems.Except(vernacular)]);
            SetList(container.CurrentAnalysisWritingSystems, analysis);
            SetList(container.CurrentVernacularWritingSystems, vernacular);
            if (substitutedDefaultAnalysis) MoveReversalIndexToDefaultAnalysis(cache, analysis[0]);
        });
        cache.ActionHandlerAccessor.Commit();
    }

    private static void SetList(ICollection<CoreWritingSystemDefinition> list, List<CoreWritingSystemDefinition> wanted)
    {
        if (list.SequenceEqual(wanted)) return;
        list.Clear();
        foreach (var ws in wanted) list.Add(ws);
    }

    // CreateNewLangProj creates a reversal index for the default analysis WS, which was "en" only because
    // the requested default couldn't be passed in. Give the requested default the index instead.
    private static void MoveReversalIndexToDefaultAnalysis(LcmCache cache, CoreWritingSystemDefinition defaultAnalysis)
    {
        cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(defaultAnalysis.Handle);
        if (cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Any(ws => ws.Id == "en")) return;
        var englishIndex = cache.LangProject.LexDbOA.ReversalIndexesOC.FirstOrDefault(ri => ri.WritingSystem == "en");
        if (englishIndex is not null) cache.LangProject.LexDbOA.ReversalIndexesOC.Remove(englishIndex);
    }

    private static void NewProject(IThreadedProgress progress,
        string projectName,
        ILcmDirectories lcmDirectories,
        ISynchronizeInvoke syncInvoke,
        CoreWritingSystemDefinition analysisWs,
        CoreWritingSystemDefinition vernacularWs,
        string uiWs,
        HashSet<CoreWritingSystemDefinition> additionalAnalysisWss,
        HashSet<CoreWritingSystemDefinition> additionalVernacularWss)
    {
        // After the default analysis/vernacular definitions and the string UI writing system,
        // CreateNewLangProj takes the additional analysis then additional vernacular writing systems.
        LcmCache.CreateNewLangProj(progress,
            [projectName, lcmDirectories, syncInvoke, analysisWs, vernacularWs, uiWs, additionalAnalysisWss, additionalVernacularWss]);
    }
}
