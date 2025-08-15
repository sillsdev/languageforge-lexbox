using MiniLcm.Models;

namespace MiniLcm.Project;

public interface IProjectImport
{
    Task<IProjectIdentifier> Import(IProjectIdentifier project);
}
