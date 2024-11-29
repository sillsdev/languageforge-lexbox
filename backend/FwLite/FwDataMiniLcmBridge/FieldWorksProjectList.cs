using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.Extensions.Options;
using MiniLcm.Models;

namespace FwDataMiniLcmBridge;

public class FieldWorksProjectList(IOptions<FwDataBridgeConfig> config)
{
    protected readonly IOptions<FwDataBridgeConfig> _config = config;

    public virtual IEnumerable<IProjectIdentifier> EnumerateProjects()
    {
        if (!Directory.Exists(_config.Value.ProjectsFolder)) Directory.CreateDirectory(_config.Value.ProjectsFolder);
        foreach (var directory in Directory.EnumerateDirectories(_config.Value.ProjectsFolder))
        {
            var projectName = Path.GetFileName(directory);
            if (string.IsNullOrEmpty(projectName)) continue;
            if (!File.Exists(Path.Combine(directory, projectName + ".fwdata"))) continue;
            yield return new FwDataProject(projectName, _config.Value.ProjectsFolder);
        }
    }

    public virtual FwDataProject? GetProject(string name)
    {
        return EnumerateProjects().OfType<FwDataProject>().FirstOrDefault(p => p.Name == name);
    }
}
