using MiniLcm;
using Mono.Unix.Native;

namespace FwDataMiniLcmBridge.LcmUtils;

public class FieldWorksProjectList
{
    public static IEnumerable<IProjectIdentifier> EnumerateProjects()
    {
        foreach (var directory in Directory.EnumerateDirectories(ProjectLoader.ProjectFolder))
        {
            var projectName = Path.GetFileName(directory);
            if (string.IsNullOrEmpty(projectName)) continue;
            if (!File.Exists(Path.Combine(directory, projectName + ".fwdata"))) continue;
            yield return new FwDataProject(projectName);
        }
    }
}

public class FwDataProject(string name) : IProjectIdentifier
{
    public string Name { get; } = name;
    public string Origin { get; } = "FieldWorks";
}
