using MiniLcm.Models;

namespace FwDataMiniLcmBridge;

/// <summary>
///
/// </summary>
/// <param name="name">project name, also the file name, and project folder name</param>
/// <param name="projectsPath">
/// path where the project folder is located,
/// e.g. "/data/projects/" not "/data/projects/MyProject/"
/// </param>
public class FwDataProject(string name, string projectsPath) : IProjectIdentifier
{
    public string Name { get; } = name;
    public string FileName { get; } = name + ".fwdata";
    public string FilePath { get; } = Path.Combine(projectsPath, name, name + ".fwdata");
    public string ProjectFolder => Path.Combine(projectsPath, name);
    /// <summary>
    /// A path to the projects folder, there must be a folder with the same name as the project name and a fwdata file in it.
    /// eg: Project name : "MyProject", path: "/data/projects/" is the value of this property, then the fwdata file must be in /data/projects/MyProject/MyProject.fwdata
    /// </summary>
    public string ProjectsPath { get; } = projectsPath;
    public ProjectDataFormat DataFormat => ProjectDataFormat.FwData;
}
