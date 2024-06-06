using FwDataMiniLcmBridge.LcmUtils;
using MiniLcm;

namespace FwDataMiniLcmBridge;

public class FieldWorksProjectList
{
    public static IEnumerable<IProjectIdentifier> EnumerateProjects()
    {
        foreach (var directory in Directory.EnumerateDirectories(ProjectLoader.ProjectFolder))
        {
            var projectName = Path.GetFileName(directory);
            if (string.IsNullOrEmpty(projectName)) continue;
            if (!File.Exists(Path.Combine(directory, projectName + ".fwdata"))) continue;
            yield return new FwDataProject(projectName, projectName + ".fwdata");
        }
    }

    public static FwDataProject? GetProject(string name)
    {
        return EnumerateProjects().OfType<FwDataProject>().FirstOrDefault(p => p.Name == name);
    }
}
