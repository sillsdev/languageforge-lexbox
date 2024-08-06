using System.Diagnostics;
using System.Runtime.InteropServices;
using SIL.LCModel;
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
}

public class ProjectLoader : IProjectLoader
{
    public static string DataFolder =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            @"C:\ProgramData\SIL\FieldWorks" :
            Path.Join(Environment.GetEnvironmentVariable("XDG_DATA_HOME") ?? Path.Join(Environment.GetEnvironmentVariable("HOME"), ".local", "share"), "fieldworks");
    public static string ProjectFolder = Path.Join(DataFolder, "Projects");
    private static string TemplatesFolder { get; } = Path.Join(DataFolder, "Templates");
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
}
