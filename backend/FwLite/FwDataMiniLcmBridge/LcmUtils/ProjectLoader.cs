using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using SIL.LCModel;
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
        NewProject(progress,
            project.Name,
            lcmDirectories,
            progress.SynchronizeInvoke,
            analysisDefinitions[0],
            vernacularDefinitions[0],
            uiWs,
            AdditionalWritingSystems(analysisDefinitions),
            AdditionalWritingSystems(vernacularDefinitions));
        return LoadCache(project);
    }

    private static CoreWritingSystemDefinition CreateWritingSystemDefinition(string ws) => new(ws) { Id = ws };

    // The default writing system (the first of the list) is passed to CreateNewLangProj on its own, so
    // the "additional" set is the whole list as a hash set with that default removed.
    private static HashSet<CoreWritingSystemDefinition> AdditionalWritingSystems(List<CoreWritingSystemDefinition> definitions)
    {
        var additional = definitions.ToHashSet();
        additional.Remove(definitions[0]);
        return additional;
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
