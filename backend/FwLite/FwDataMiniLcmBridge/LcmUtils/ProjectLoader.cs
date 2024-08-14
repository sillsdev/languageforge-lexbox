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
    /// <returns></returns>
    LcmCache LoadCache(string fileName);

    LcmCache NewProject(string fileName, string analysisWs, string vernacularWs);
}

public class ProjectLoader(IOptions<FwDataBridgeConfig> config) : IProjectLoader
{
    private string ProjectFolder => config.Value.ProjectsFolder;
    private string TemplatesFolder => config.Value.TemplatesFolder;
    private static bool _init;

    public static void Init()
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


    /// <summary>
    /// loads a fwdata file that lives in the project folder C:\ProgramData\SIL\FieldWorks\Projects
    /// </summary>
    /// <param name="fileName">could be the full path or just the file name, the path will be ignored, must include the extension</param>
    /// <returns></returns>
    public LcmCache LoadCache(string fileName)
    {
        Init();
        fileName = Path.GetFileName(fileName);
        var projectFilePath = Path.Combine(ProjectFolder, Path.GetFileNameWithoutExtension(fileName), fileName);
        var lcmDirectories = new LcmDirectories(ProjectFolder, TemplatesFolder);
        var progress = new LcmThreadedProgress();
        var cache = LcmCache.CreateCacheFromLocalProjectFile(projectFilePath,
            null,
            new LfLcmUi(progress.SynchronizeInvoke),
            lcmDirectories,
            new LcmSettings(),
            progress
        );
        return cache;
    }

    public LcmCache NewProject(string fileName, string analysisWs, string vernacularWs)
    {
        Init();
        var lcmDirectories = new LcmDirectories(ProjectFolder, TemplatesFolder);
        var progress = new LcmThreadedProgress();
        NewProject(progress,
            Path.GetFileNameWithoutExtension(fileName),
            lcmDirectories,
            progress.SynchronizeInvoke,
            new CoreWritingSystemDefinition(analysisWs) { Id = analysisWs },
            new CoreWritingSystemDefinition(vernacularWs) { Id = vernacularWs });
        return LoadCache(fileName);
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
