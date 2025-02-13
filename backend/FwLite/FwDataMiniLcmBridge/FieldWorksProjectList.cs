using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.Extensions.Options;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.Project;

namespace FwDataMiniLcmBridge;

public class FieldWorksProjectList(IOptions<FwDataBridgeConfig> config, FwDataFactory fwDataFactory) : IProjectProvider
{
    public ProjectDataFormat DataFormat => ProjectDataFormat.FwData;
    protected readonly IOptions<FwDataBridgeConfig> _config = config;
    IEnumerable<IProjectIdentifier> IProjectProvider.ListProjects()
    {
        return EnumerateProjects();
    }

    IProjectIdentifier? IProjectProvider.GetProject(string name)
    {
        return GetProject(name);
    }

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

    public IMiniLcmApi OpenProject(IProjectIdentifier project, bool saveOnDispose = true)
    {
        if (project is not FwDataProject fwDataProject) throw new ArgumentException("Project is not a fwdata project");
        return fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, saveOnDispose);
    }
}
