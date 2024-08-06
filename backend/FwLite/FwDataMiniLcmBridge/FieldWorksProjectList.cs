using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.Extensions.Options;
using MiniLcm;

namespace FwDataMiniLcmBridge;

public class FieldWorksProjectList(IOptions<FwDataBridgeConfig> config)
{
    public IEnumerable<IProjectIdentifier> EnumerateProjects()
    {
        foreach (var directory in Directory.EnumerateDirectories(config.Value.ProjectsFolder))
        {
            var projectName = Path.GetFileName(directory);
            if (string.IsNullOrEmpty(projectName)) continue;
            if (!File.Exists(Path.Combine(directory, projectName + ".fwdata"))) continue;
            yield return new FwDataProject(projectName, projectName + ".fwdata");
        }
    }

    public FwDataProject? GetProject(string name)
    {
        return EnumerateProjects().OfType<FwDataProject>().FirstOrDefault(p => p.Name == name);
    }
}
