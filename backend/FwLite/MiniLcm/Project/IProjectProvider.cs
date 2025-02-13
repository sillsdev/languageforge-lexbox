using MiniLcm.Models;

namespace MiniLcm.Project;

public interface IProjectProvider
{
    ProjectDataFormat DataFormat { get; }
    IEnumerable<IProjectIdentifier> ListProjects();
    IProjectIdentifier? GetProject(string name);
    IMiniLcmApi OpenProject(IProjectIdentifier project, bool saveChangesOnDispose = true);
}
