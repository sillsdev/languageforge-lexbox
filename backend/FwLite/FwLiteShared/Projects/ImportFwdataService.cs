using Microsoft.JSInterop;
using MiniLcm.Models;
using MiniLcm.Project;

namespace FwLiteShared.Projects;

public class ImportFwdataService(IEnumerable<IProjectProvider> projectProviders, IProjectImport? miniLcmImport = null)
{
    private IProjectProvider? FwDataProjectProvider =>
        projectProviders.FirstOrDefault(p => p.DataFormat == ProjectDataFormat.FwData);

    [JSInvokable]
    public async Task<IProjectIdentifier> Import(string projectName)
    {
        if (miniLcmImport is null) throw new InvalidOperationException("MiniLcmImport is not available and import is not supported in this version");
        if (FwDataProjectProvider is null)
        {
            throw new InvalidOperationException("FwData Project provider is not available");
        }

        var fwDataProject = FwDataProjectProvider.GetProject(projectName);
        if (fwDataProject is null)
        {
            throw new InvalidOperationException($"Project {projectName} not found.");
        }

        return await miniLcmImport.Import(fwDataProject);
    }
}
