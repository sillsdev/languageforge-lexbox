using SIL.LCModel;
using SIL.WritingSystems;

namespace FwDataMiniLcmBridge.LcmUtils;

public class ProjectLoader
{
    private static string ProjectFolder { get; } = @"C:\ProgramData\SIL\FieldWorks\Projects";
    private static string TemplatesFolder { get; } = @"C:\ProgramData\SIL\FieldWorks\Templates";
    private static bool _init;

    public static void Init()
    {
        if (_init)
        {
            return;
        }

        Icu.Wrapper.Init();
        Sldr.Initialize();
        _init = true;
    }


    /// <summary>
    /// loads a fwdata file that lives in the project folder C:\ProgramData\SIL\FieldWorks\Projects
    /// </summary>
    /// <param name="fileName">could be the full path or just the file name, the path will be ignored, must include the extension</param>
    /// <returns></returns>
    public static LcmCache LoadCache(string fileName)
    {
        Init();
        fileName = Path.GetFileName(fileName);
        var projectFilePath = Path.Combine(ProjectFolder, Path.GetFileNameWithoutExtension(fileName), fileName);
        var lcmDirectories = new LcmDirectories(ProjectFolder, TemplatesFolder);
        var progress = new LcmThreadedProgress();
        Console.WriteLine("Creating cache");
        var cache = LcmCache.CreateCacheFromLocalProjectFile(projectFilePath,
            null,
            new LfLcmUi(progress.SynchronizeInvoke),
            lcmDirectories,
            new LcmSettings(),
            progress
        );
        Console.WriteLine("Done creating cache");
        return cache;
    }
}
