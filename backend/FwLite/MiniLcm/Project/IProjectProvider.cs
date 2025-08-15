using MiniLcm.Models;

namespace MiniLcm.Project;

public interface IProjectProvider
{
    ProjectDataFormat DataFormat { get; }
    IEnumerable<IProjectIdentifier> ListProjects();
    IProjectIdentifier? GetProject(string name);
    ValueTask<IMiniLcmApi> OpenProject(IProjectIdentifier project, IServiceProvider serviceProvider, bool saveChangesOnDispose = true);
}
