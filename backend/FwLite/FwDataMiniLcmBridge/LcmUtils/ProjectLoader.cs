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

    LcmCache NewProject(FwDataProject project, string analysisWs, string vernacularWs);
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

    public virtual LcmCache NewProject(FwDataProject project, string analysisWs, string vernacularWs)
    {
        Init();
        var lcmDirectories = new LcmDirectories(project.ProjectsPath, TemplatesFolder);
        var progress = new LcmThreadedProgress();
        NewProject(progress,
            project.Name,
            lcmDirectories,
            progress.SynchronizeInvoke,
            new CoreWritingSystemDefinition(analysisWs) { Id = analysisWs },
            new CoreWritingSystemDefinition(vernacularWs) { Id = vernacularWs });
        return LoadCache(project);
    }

    private static void NewProject(IThreadedProgress progress,
        string projectName,
        ILcmDirectories lcmDirectories,
        ISynchronizeInvoke syncInvoke,
        CoreWritingSystemDefinition analysisWs,
        CoreWritingSystemDefinition vernacularWs)
    {
        LcmCache.CreateNewLangProj(progress, [projectName, lcmDirectories, syncInvoke, analysisWs, vernacularWs]);
    }
}
